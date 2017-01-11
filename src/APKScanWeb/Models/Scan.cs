using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APKScanWeb;
using Cassandra;
using Newtonsoft.Json;
using Cassandra.Mapping;
using System.IO;
using APKScanSharedClasses;
using System.Threading;

namespace APKScanWeb.Models
{
    public class Scan
    {
        private static bool isBGTaskRunning = false;
        DataLayer dl;
        public Scan()
        {
            try
            {
                dl = DataLayer.getInstance();
            }
            catch
            {
                throw new Exception("Error while getting DataLayer instance!");
            }
        }
        public bool existsInRedisCache(string hash)
        {
            if (!dl.redis.KeyExists("cache:" + hash))
                return false;
            return true;
        }
        public bool existsInCassandra(string hash)
        {
            var query = dl.cassandra.Prepare("SELECT count(*) FROM files WHERE hash = ? limit 5000000");
            var statement = query.Bind(hash);
            statement.SetConsistencyLevel(ConsistencyLevel.Quorum);
            var result = dl.cassandra.Execute(statement);
            var row = result.First();
            var val = row.GetValue<Int64>(0); //Type must be Int64 (bigint)
            if (val == 0) //first row
                return false;
            return true;
        }
        public Result getScanResultFromRedisCache(string hash)
        {
            if(existsInRedisCache(hash))
            {
                var redisResult = dl.redis.StringGet("cache:" + hash);
                return JsonConvert.DeserializeObject<Result>(redisResult);
            }
            return null;
        }
        public Result getScanResultFromCassandra(string hash)
        {
            if (existsInCassandra(hash))
            {
                IMapper mapper = new Mapper(dl.cassandra);
                Result result = mapper.Single<Result>("SELECT hash,filename,hits,av FROM files WHERE hash = ?", hash);
                return result;
            }
            return null;
        }
        public Result getScanResult(string hash)
        {
            //first check redis cache for data
            var redisResult = getScanResultFromRedisCache(hash);
            if (redisResult != null)
                return redisResult; 

            //if there is nothing in the redis cache then check in cassandra db
            var cassandraResult = getScanResultFromCassandra(hash);
            if (cassandraResult != null)
            {
                //data is present in the cassandra db so push it to the redis cache
                addScanResultToRedisCache(cassandraResult);

                //return the data
                return cassandraResult;
            }
                
            return null;
        }
        public bool uploadToDirectory(string directory, string filename, byte [] data)
        {
            //write the bytes of the file to the stream
            FileStream fs = new FileStream(directory + filename, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            if (fs.CanWrite)
                bw.Write(data, 0, data.Length);
            else
                return false;
            return true;
        }
        public long addFileToRedisSendQueue(string filename, string hash, string ip = "")
        {
            //Result should not be used as it is not a proper object
            //ip is currently not used
            RedisSend obj = new RedisSend();

            //form the object and add the data needed
            obj.filename = filename;
            obj.hash = hash;
            obj.upload_ip = ip;
            obj.upload_date = DateTime.UtcNow;

            //serialize the object and push it to the list
            string data = JsonConvert.SerializeObject(obj);
            long queueNumber = dl.redis.ListLeftPush("send", data);
            return queueNumber;
        }
        public bool addScanResultToRedisCache(RedisReceive result)
        {
            Result r = new Result();

            r.hash = result.hash;
            r.av = result.av_results;
            r.filename.Add(result.filename);
            r.hits = 0;
            r.upload_ip.Add(result.upload_ip);

            addScanResultToRedisCache(r);

            return true;
        }
        public bool addScanResultToRedisCache(Result result)
        {
            //serialize the result object
            string data = JsonConvert.SerializeObject(result);

            //write it to cache
            var key = dl.redis.StringSet("cache:" + result.hash, data);
            if (!key)
                return false;

            //set the key expiry to 1 day fixed in cache
            dl.redis.KeyExpire("cache:" + result.hash, TimeSpan.FromDays(1));

            return true;
        }
        public bool addScanResultToCassandra(RedisReceive result)
        {
            var query = dl.cassandra.Prepare("INSERT INTO files (hash, filename, upload_ip, hits, av) VALUES(?, ?, ?, 0, ?);");

            var statement = query.Bind(result.hash, result.filename, result.upload_ip, result.av_results);
            statement.SetConsistencyLevel(ConsistencyLevel.Quorum);
            var res = dl.cassandra.Execute(statement);

            return true;
        }
        
        
    }
}
