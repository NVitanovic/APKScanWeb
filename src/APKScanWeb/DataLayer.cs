using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration.Json;
using Newtonsoft.Json;
using MongoDB.Driver;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace APKScanWeb
{
    public class DataLayer
    {
        public Cluster cassandraCluster = null;
        public ISession cassandra = null;
        public ConnectionMultiplexer redisCluster = null;
        public IDatabase redis = null;
        public MongoClient mongoCluster = null;
        public IMongoDatabase mongo = null;
        public static DataLayer singleton = null;
        private static object  lockObj = new object();

        public MySqlConnection mysql = null;
        
        public static DataLayer getInstance()
        {
            if (singleton == null)
            {
                lock (lockObj)
                {
                    if (singleton == null)
                        return singleton = new DataLayer(Program.config);
                    else
                        return singleton;
                }
            }
            else
                return singleton;
        }
        private DataLayer(Models.Configuration config)
        {
            if (mysql == null)
            {
                //string sql = String.Format("server={0};user={1};database={2};password={3};", config.mysql.hostname, config.mysql.username, config.mysql.database, config.mysql.password);
                string sql = "server=127.0.0.1;user=root;database=apkscan;port=3306;password=;SslMode=none;";
                mysql = new MySqlConnection(sql);
                if (mysql.State == System.Data.ConnectionState.Closed)
                        mysql.Open();
            }

            /*if (cassandraCluster == null)
            {
                cassandraCluster = Cluster.Builder()
                                   .AddContactPoints(config.cassandra.servers.ToArray())
                                   .Build();

                if (cassandra == null) //inicijalizacija sesije
                    cassandra = cassandraCluster.Connect(config.cassandra.keyspace);
            }*/

            /*if(redisCluster == null)
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
            }*/
            
            /*if(mongoCluster == null)
            {
                string mongoConfig = "mongodb://";
                for (int i = 0; i < config.mongo.servers.Count; i++)
                    if (i == config.mongo.servers.Count - 1)
                        mongoConfig += config.mongo.servers[i];
                    else
                        mongoConfig += config.mongo.servers[i] + ",";
                mongoConfig += "/?replicaSet=" + config.mongo.replicaset;
                mongoCluster = new MongoClient(mongoConfig);
                mongo = mongoCluster.GetDatabase(config.mongo.database);
            }*/

            
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
