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

namespace APKScanWeb.Models
{
    public class Scan
    {
        DataLayer dl;
        public Scan()
        {
            try
            {
                dl = DataLayer.GetInstance();
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
            var query = dl.cassandra.Prepare("SELECT count(*) FROM files WHERE hash = ?");
            var statement = new BatchStatement().Add(query.Bind(hash));
            var result = dl.cassandra.Execute(statement);
            var row = result.First();
            if (row.GetValue<int>("count(*)") == 0)
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
        public long addFileToRedisQueue(string filename, string hash, string ip = "")
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
            var query = dl.cassandra.Prepare("INSERT INTO files (hash, filename, upload_ip, hits, av) VALUES(?, ?, ?, 0, ?)");
            var statement = new BatchStatement()
                .Add(query.Bind(result.hash))
                .Add(query.Bind(result.filename))
                .Add(query.Bind(result.upload_ip))
                .Add(query.Bind(result.av_results));
            var res = dl.cassandra.Execute(statement);

            return true;
        }
    }
}
