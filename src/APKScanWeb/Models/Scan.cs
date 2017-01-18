using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APKScanWeb;
using APKScanWeb.Models;
using APKScanWeb.Models.Entities;
using Cassandra;
using Newtonsoft.Json;
using Cassandra.Mapping;
using System.IO;
using APKScanSharedClasses;
using System.Threading;
using StackExchange.Redis;

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
                Result result = mapper.Single<Result>("SELECT hash,filename,hits,av,upload_ip FROM files WHERE hash = ?", hash);
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
        public bool uploadToDirectory(string directory, string filename, byte[] data)
        {
            //check if data length is 0
            if (data.Length == 0)
                return false;
            //write the bytes of the file to the stream
            try
            {
                FileStream fs = new FileStream(directory + filename, FileMode.Create);
                BinaryWriter bw = new BinaryWriter(fs);

                if (fs.CanWrite)
                    bw.Write(data, 0, data.Length);
                else
                {
                    bw.Flush();
                    fs.Flush();
                    bw.Dispose();
                    fs.Dispose();
                    return false;
                }
                bw.Flush();
                fs.Flush();
                bw.Dispose();
                fs.Dispose();
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Data);
            }
            return false;
        }
        public long addFileToRedisSendQueue(string filename, string hash, string ip = "0.0.0.0")
        {
            //Result should not be used as it is not a proper object
            //ip is currently not used
            RedisSend obj = new RedisSend();
            ISubscriber sub = dl.redisCluster.GetSubscriber();

            //form the object and add the data needed
            obj.filename = filename;
            obj.hash = hash;
            obj.upload_ip = ip == null?"0.0.0.0":ip;
            obj.upload_date = DateTime.UtcNow;

            //serialize the object and push it to the list
            string data = JsonConvert.SerializeObject(obj);
            long queueNumber = dl.redis.ListLeftPush("send", data);
            //announce that the item was pushed to the list
            sub.Publish("send", "api");
            return queueNumber;
        }
        public bool addScanResultToRedisCache(RedisReceive result)
        {
            addScanResultToRedisCache(getScanResultFromCassandra(result.hash));

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
            List<string> upload_ip = new List<string>();
            upload_ip.Add(result.upload_ip == null ? "0.0.0.0" : result.upload_ip);
            List<string> filename = new List<string>();
            filename.Add(result.filename);
            int hits = 0;
            Dictionary<string, string> av = new Dictionary<string, string>(result.av_results);

            //get the existing result
            var existingResult = getScanResultFromCassandra(result.hash);

            if(existingResult != null)
            {
                //for every existing ip check if it matches the new ip if it does not then just add it to the existing pile
                for(int i = 0; i < existingResult.upload_ip.Count; i++)
                {
                    //check if the ip is already in the list
                    if (upload_ip.IndexOf(existingResult.upload_ip[i]) != -1)
                        continue;

                    //add the value that does not exist in the ip list
                    upload_ip.Add(existingResult.upload_ip[i]);
                }
                // do the same for filenames
                for (int i = 0; i < existingResult.filename.Count; i++)
                {
                    //check if the filename is already in the list
                    if (filename.IndexOf(existingResult.filename[i]) != -1)
                        continue;

                    //add the value that does not exist in the filename list
                    filename.Add(existingResult.filename[i]);
                }

                //set the hits value to the same one
                hits = existingResult.hits;

                //now the av check
                foreach(var x in existingResult.av)
                {
                    //only add the missing values and accept all new scans
                    if(!av.ContainsKey(x.Key))
                        av.Add(x.Key, x.Value);
                }
            }

            var query = dl.cassandra.Prepare("INSERT INTO files (hash, filename, hits, upload_ip, av) VALUES(?, ?, ?, ?, ?);");

            var statement = query.Bind(result.hash, filename, hits, upload_ip, av);
            statement.SetConsistencyLevel(ConsistencyLevel.Quorum);
            var res = dl.cassandra.Execute(statement);

            return true;
        }
        
        
    }
}
