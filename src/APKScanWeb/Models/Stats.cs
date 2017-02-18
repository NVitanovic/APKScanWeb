using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB;
using MongoDB.Bson;

namespace APKScanWeb.Models
{
    public class Stats
    {
        private DataLayer dl;
        public Stats()
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
        private bool addToCollection(string data, string collection, string ip)
        {
            if (String.IsNullOrEmpty(data))
                return false;

            if (String.IsNullOrEmpty(ip))
                ip = "0.0.0.0";

            try
            {
                var bsonDoc = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(data);
                bsonDoc.Set("created_at", DateTime.UtcNow);
                bsonDoc.Set("upload_ip", ip);
                var mongoCol = dl.mongo.GetCollection<BsonDocument>(collection);
                mongoCol.InsertOne(bsonDoc);
            }
            catch
            {
                return false;
            }
   
            return true;
        }
        public bool addToScanCollection(string data, string ip = "0.0.0.0")
        {
            return addToCollection(data, "scan", ip);
        }
        public bool addToResultCollection(string data, string ip = "0.0.0.0")
        {
            return addToCollection(data, "result", ip);
        }
    }
}
