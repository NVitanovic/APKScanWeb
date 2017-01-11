using APKScanSharedClasses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APKScanWeb.Models
{
    public class BackgrounRedisWorker
    {
        private static object lockObj = new object();
        private static BackgrounRedisWorker singleton = null;
        private Scan scanModel;
        private DataLayer dl;
        public static BackgrounRedisWorker getInstance()
        {
            if (singleton == null)
            {
                lock (lockObj)
                {
                    if (singleton == null)
                        return singleton = new BackgrounRedisWorker();
                    else
                        return singleton;
                }
            }
            else
                return singleton;
        }
        private BackgrounRedisWorker()
        {
            scanModel = new Scan();
            dl = DataLayer.getInstance();

            Thread t = new Thread(backgroundRedisCheckThread);
            t.Start();
        }
        //NOTE: This function will run in a thread and should not be called by 
        //      any means by other functions other than runBackgrounRedisReceiveCheck()!
        private void backgroundRedisCheckThread()
        {
            while (true)
            {
                //get the data from Redis receive queue
                string rawData = dl.redis.ListRightPop("receive");
                //if there is data
                if (rawData != null)
                {
                    //deserialize object
                    RedisReceive obj = JsonConvert.DeserializeObject<RedisReceive>(rawData);

                    //Add to the Cassandra Server
                    if (!scanModel.addScanResultToCassandra(obj))
                        throw new Exception("Error while trying to write to Cassandra server from the background Thread!");
                    //Add to the Redis cache
                    if (!scanModel.addScanResultToRedisCache(obj))
                        throw new Exception("Error while trying to write to Redis server from the background Thread!");
                }
                Thread.Sleep(10000);
            }
        }
    }
}
