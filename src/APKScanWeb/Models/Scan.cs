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
            var redisResult = getScanResultFromRedisCache(hash);
            if (redisResult != null)
                return redisResult; 
            var cassandraResult = getScanResultFromCassandra(hash);
            if (cassandraResult != null)
                return cassandraResult; //TODO: add to Redis cache
            return null;
        }
        public bool uploadToDirectory(string directory, string filename, byte [] data)
        {
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

            string data = JsonConvert.SerializeObject(obj);
            long queueNumber = dl.redis.ListLeftPush("send", data);
            return queueNumber;
        }
    }
}
