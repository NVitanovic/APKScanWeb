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
        private bool addToCollection(string data, string collection)
        {
            if (String.IsNullOrEmpty(data))
                return false;

            try
            {
                var bsonDoc = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(data);
                var mongoCol = dl.mongo.GetCollection<BsonDocument>("scan");
                mongoCol.InsertOne(bsonDoc);
            }
            catch
            {
                return false;
            }
   
            return true;
        }
        public bool addToScanCollection(string data)
        {
            return addToCollection(data, "scan");
        }
        public bool addToResultCollection(string data)
        {
            return addToCollection(data, "result");
        }
    }
}
