using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.IO;

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
        public JsonResult getStats()
        {
            var cache = dl.redis.StringGet("stats:cache");
            if (cache.IsNullOrEmpty) //no data in Redis update it and return current data
            { 
                var mongoStats = getStatsFromMongo();
                dl.redis.StringSet("stats:cache", mongoStats.Value.ToJson());
                dl.redis.KeyExpire("stats:cache", TimeSpan.FromDays(1));
                return mongoStats;
            }

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(cache);
            return new JsonResult(result);
        }
        private JsonResult getStatsFromMongo()
        {
            //---Get Country stats---
            var countries = analyticsCountries();
            BsonDocument countriesJson = new BsonDocument();
            foreach (var item in countries)
            {
                var key = item["_id"].ToString();
                var val = item["count"].ToString();
                countriesJson.Add(key, val);
            }

            //---Get OS stats---
            var os = analyticsOS();
            BsonDocument osJson = new BsonDocument();
            foreach (var item in os)
            {
                var key = item["_id"].ToString();
                var val = item["count"].ToString();
                osJson.Add(key, val);
            }

            //---Get Browser stats---
            var browser = analyticsBrowser();
            BsonDocument browserJson = new BsonDocument();
            foreach (var item in browser)
            {
                var key = item["_id"].ToString();
                var val = item["count"].ToString();
                browserJson.Add(key, val);
            }

            var device = analyticsDeviceType();
            BsonDocument deviceJson = new BsonDocument();
            foreach (var item in device)
            {
                String key;

                if (item["_id"].BsonType != BsonType.Null)
                    key = item["_id"].ToString();
                else
                    key = "desktop";

                var val = item["count"].ToString();
                deviceJson.Add(key, val);
            }

            var fileTypes = analyticsFileMimeTypes();
            BsonDocument fileTypesJson = new BsonDocument();
            foreach (var item in fileTypes)
            {
                var key = item["_id"].ToString();
                var val = item["count"].ToString();
                fileTypesJson.Add(key, val);
            }

            var fileSizesJson = analyticsFileSize();

            var scansJson = weeklyScans();

            var totalUploadsJson = analyticsTotalUniqueFilesSubmitted();

            var totalDetectionsJson = analyticsTotalDetections();

            var totalScansJson = analyticsTotalScans();

            var totalFileSizeJson = analyticsTotalFileSize() / 1024 * 1024; //MegaBytes

            return new JsonResult(new {
                scans = totalScansJson,
                uploads = totalUploadsJson,
                detections = totalDetectionsJson,
                totalfilesize = totalFileSizeJson,
                weekscans = scansJson.ToDictionary(),
                countries = countriesJson.ToDictionary(),
                os = osJson.ToDictionary(),
                browser = browserJson.ToDictionary(),
                device = deviceJson.ToDictionary(),
                filetypes = fileTypesJson.ToDictionary(),
                filesizes = fileSizesJson.ToDictionary()
                
            });
        }
        //-----------------------------------------------------------------------------------
        private List<BsonDocument> analyticsCountries()
        {
            var collection = dl.mongo.GetCollection<BsonDocument>("result");
            var aggregate = collection.Aggregate().Group(new BsonDocument { { "_id", "$fingerprint.geoLocation.country_name" }, { "count", new BsonDocument("$sum", 1) } });
            var results = aggregate.ToList();
            return results;
        }

        private List<BsonDocument> analyticsOS()
        {
            var collection = dl.mongo.GetCollection<BsonDocument>("result");
            var aggregate = collection.Aggregate().Group(new BsonDocument { { "_id", "$fingerprint.OS" }, { "count", new BsonDocument("$sum", 1) } });
            var results = aggregate.ToList();
            return results;
        }

        private List<BsonDocument> analyticsBrowser()
        {
            var collection = dl.mongo.GetCollection<BsonDocument>("result");
            var aggregate = collection.Aggregate().Group(new BsonDocument { { "_id", "$fingerprint.browser" }, { "count", new BsonDocument("$sum", 1) } });
            var results = aggregate.ToList();
            return results;
        }

        private List<BsonDocument> analyticsDeviceType()
        {
            var collection = dl.mongo.GetCollection<BsonDocument>("result");
            var aggregate = collection.Aggregate().Group(new BsonDocument { { "_id", "$fingerprint.deviceType" }, { "count", new BsonDocument("$sum", 1) } });
            var results = aggregate.ToList();
            return results;
        }

        private List<BsonDocument> analyticsFileMimeTypes()
        {
            var collection = dl.mongo.GetCollection<BsonDocument>("scan");
            var aggregate = collection.Aggregate().Group(new BsonDocument { { "_id", "$file.type" }, { "count", new BsonDocument("$sum", 1) } });
            var results = aggregate.ToList();
            return results;
        }

        private BsonDocument weeklyScans()
        {
            var theDay = DateTime.UtcNow;
            var collection = dl.mongo.GetCollection<BsonDocument>("result");
            BsonDocument x = new BsonDocument();
            x.Add("1", analyticsPerDay(theDay, collection, 0));//from 1 day before to now
            x.Add("2", analyticsPerDay(theDay, collection, -1));
            x.Add("3", analyticsPerDay(theDay, collection, -2));
            x.Add("4", analyticsPerDay(theDay, collection, -3));
            x.Add("5", analyticsPerDay(theDay, collection, -4));
            x.Add("6", analyticsPerDay(theDay, collection, -5));
            x.Add("7", analyticsPerDay(theDay, collection, -6));
            return x;
        }

        private int analyticsPerDay(DateTime theDay, IMongoCollection<BsonDocument> collection, int day)
        {
            var filter = Builders<BsonDocument>.Filter.Lte(new StringFieldDefinition<BsonDocument, BsonDateTime>("created_at"), new BsonDateTime(theDay.AddDays(day))) &
                         Builders<BsonDocument>.Filter.Gte(new StringFieldDefinition<BsonDocument, BsonDateTime>("created_at"), new BsonDateTime(theDay.AddDays(day - 1)));
            return Convert.ToInt32(collection.Find(filter).Count());
        }

        private int analyticsTotalUniqueFilesSubmitted()
        {
            var collection = dl.mongo.GetCollection<BsonDocument>("scan");
            var aggregate = collection.Aggregate()
                .Group(new BsonDocument { { "_id", "$file.MD5hash" }, { "count", new BsonDocument("$sum", 1) } });
            int results = aggregate.ToList().Count();
            return results;
        }

        private int analyticsTotalDetections()
        {
            var collection = dl.mongo.GetCollection<BsonDocument>("result");
            var aggregate = collection.Aggregate()
            .Group(new BsonDocument { { "_id", 0 }, { "count", new BsonDocument("$sum", "$file.detections") } });

            return aggregate.ToList()[0]["count"].ToInt32();
        }
        
        private int analyticsTotalScans()
        {
            var collection = dl.mongo.GetCollection<BsonDocument>("result");
            var aggregate = collection.Aggregate()
            .Group(new BsonDocument { { "_id", 0 }, { "count", new BsonDocument("$sum", 1) } });

            return aggregate.ToList()[0]["count"].ToInt32();
        }
        private int analyticsTotalFileSize()
        {
            var collection = dl.mongo.GetCollection<BsonDocument>("scan");
            var aggregate = collection.Aggregate()
            .Group(new BsonDocument { { "_id", 0 }, { "count", new BsonDocument("$sum", "$file.size") } });

            return aggregate.ToList()[0]["count"].ToInt32();
        }
        private BsonDocument analyticsFileSize()
        {
            BsonDocument x = new BsonDocument();
            var collection = dl.mongo.GetCollection<BsonDocument>("scan");
            x.Add("<1MB", collection.Find(Builders<BsonDocument>.Filter.Lt("file.size", 1024 * 1024)).ToList().Count());
            x.Add("<10MB", collection.Find(Builders<BsonDocument>.Filter.Lt("file.size", 10 * 1024 * 1024)).ToList().Count());
            x.Add("<50MB", collection.Find(Builders<BsonDocument>.Filter.Lt("file.size", 50 * 1024 * 1024)).ToList().Count());
            x.Add(">50MB", collection.Find(Builders<BsonDocument>.Filter.Gte("file.size", 50 * 1024 * 1024)).ToList().Count());
            return x;
        }
    }
}
