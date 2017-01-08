using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration.Json;
using Newtonsoft.Json;

namespace APKScanWeb
{
    public class DataLayer
    {
        public Cluster cassandraCluster = null;
        public ISession cassandra = null;
        public ConnectionMultiplexer redisCluster = null;
        public IDatabase redis = null;
        public static DataLayer singleton = null;
        private static object  thislock = new object();
        public static DataLayer GetInstance()
        {
            if (singleton == null)
            {
                lock (thislock)
                {
                    if (singleton == null)
                        return new DataLayer(Program.config);
                    else
                        return singleton;
                }

            }
            else
                return singleton;
        }
        private DataLayer(Models.Configuration config)
        {
            if(cassandraCluster == null)
            {
                cassandraCluster = Cluster.Builder()
                                   .AddContactPoints(config.cassandra.servers.ToArray())
                                   .Build();

                if (cassandra == null) //inicijalizacija sesije
                    cassandra = cassandraCluster.Connect(config.cassandra.keyspace);
            }

            if(redisCluster == null)
            {
                string redisConfig = "";

                for (int i = 0; i < config.redis.masters.Count; i++)
                    redisConfig += config.redis.masters[i] + ",";
                for (int i = 0; i < config.redis.slaves.Count; i++)
                    if (i == config.redis.slaves.Count - 1)
                        redisConfig += config.redis.slaves[i];
                    else
                        redisConfig += config.redis.slaves[i] + ",";

                redisCluster = ConnectionMultiplexer.Connect(redisConfig);

                if(redis == null)
                    redis = redisCluster.GetDatabase();


                //privremeno pokrecemo testSubscribe
                //testSubscribe();
            }
        }
        /*public void testSubscribe()
        {
            ISubscriber sub = redisCluster.GetSubscriber();
            sub.Subscribe("receive", (channel, message) =>
            {
                while (true)
                {
                    string work = redis.ListRightPop("receive");
                    if (work != null)
                    {
                        Console.WriteLine("Subscrb: " + (string)work);
                    }
                }
            });
       }*/
    }
}
