using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace APKScanWeb.Models
{
    public static class Helpers
    {
        public static string GetMD5HashFromFile(string fileName)
        {
            FileStream file = new FileStream(fileName, FileMode.Open);
            var md5 = MD5.Create();
            byte[] retVal = md5.ComputeHash(file);
            file.Dispose();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
        public static string GetMD5HashFromBytes(byte [] data)
        {
            var md5 = MD5.Create();
            byte[] retVal = md5.ComputeHash(data);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }

        public static string TestLatencyRedis()
        {
            DataLayer dl = DataLayer.getInstance();
            var x = dl.redis.Ping();
            return x.TotalSeconds.ToString();
        }
        public static string TestCassandra()
        {
            string x = "";
            DataLayer dl = DataLayer.getInstance();
            var rs = dl.cassandra.Execute("select * from files");
            foreach (var row in rs)
            {
                var value = row.GetValue<string>("hash");
                x += value + "\n";
                //do something with the value
            }
            return x;
        }

        public static List<string> lista = new List<string>();
        public static string WriteToSend(string data)
        {
            DataLayer dl = DataLayer.getInstance();
            ISubscriber sub = dl.redisCluster.GetSubscriber();
            string log = "ListLeftPush:";
            log += dl.redis.ListLeftPush("send", data) + " Publish:";
            log += sub.Publish("send", "");
            return log;
        }
        public static string ReadFromReceive()
        {
            DataLayer dl = DataLayer.getInstance();
            ISubscriber sub = dl.redisCluster.GetSubscriber();
            string log = "";
            while (true)
            {
                string work = dl.redis.ListRightPop("receive");
                if (work != null)
                {
                    log += work + "\n";
                }
                else
                    break;
            }
            return log;
        }
        
    }
}
