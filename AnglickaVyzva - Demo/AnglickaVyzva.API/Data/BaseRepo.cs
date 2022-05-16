using AnglickaVyzva.API.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Data
{
    public class SaveParameters
    {
        public string UserIp { get; set; }
        public string UserName { get; set; }
        public string Host { get; set; }

        public static SaveParameters Factory_ForCrone()
        {
            return new SaveParameters
            {
                UserIp = "cron",
                UserName = "cron",
                Host = "cron",
                //logHelperCredentials: new HelpersStandard.LogHelper.LogHelperCredentials(Configuration["Storage-Log-Account-Name"], Configuration["Storage-Log-Key"])
            };
        }
    }


    public class BaseRepoDefault
    {
        public static async Task SaveAll(SaveParameters saveParameters, DbContext context)
        {
            string userIP = saveParameters.UserIp;
            string userName = saveParameters.UserName;
            DateTime now = DateTime.Now;

            var modifiedOrAddedEntries = context.ChangeTracker.Entries()
                 .Where(x =>
                            x.State == EntityState.Added ||
                            x.State == EntityState.Modified
                 ).ToList();

            var logsForNewAddedEntries = new Dictionary<BaseEntity, Log>();

            foreach (var entry in modifiedOrAddedEntries)
            {
                BaseEntity a = (BaseEntity)entry.Entity;


                // Novy i upraveny zaznam
                if (a.GetType() != typeof(Log)) // Log nepotrebuje LastUpdatedX protoze se nikdy nebude upravovat
                {
                    a.LastUpdatedDate = now;
                    a.LastUpdatedIP = userIP;
                    a.LastUpdatedWith = userName;
                }

                // Novy zaznam
                if (entry.State == EntityState.Added)
                {
                    a.CreatedDate = now;
                    a.CreatedIp = userIP;
                    a.CreatedWith = userName;

                }
            }

            int changed = await context.SaveChangesAsync();

        }

        // Jmeno entity vytazene z DB je naprikad Advertisement_65sdf8rozsipanejcaj6sdf8 -> proto beru jenom cast pred podtrzitkem (pocitam s tim, ze zadna trida nema ve jmenu lomitko)
        public static string GetName(BaseEntity a)
        {
            var name = a.GetType().Name;
            name = name.Split('_')[0];

            return name;
        }

        public class OnlyPrimitiveJsonConverter : JsonConverter
        {
            private readonly Type[] _types;

            public OnlyPrimitiveJsonConverter(params Type[] types)
            {
                _types = types;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                JToken t = JToken.FromObject(value, new JsonSerializer()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });

                if (t.Type != JTokenType.Object)
                {
                    t.WriteTo(writer);
                }
                else
                {
                    JObject o = (JObject)t;

                    var propertiesToRemove = new List<string>();

                    foreach (var prop in value.GetType().GetProperties())
                    {
                        if (
                            !prop.PropertyType.IsPrimitive &&
                            !prop.PropertyType.IsValueType &&
                            prop.PropertyType != typeof(string)
                            )
                        {
                            propertiesToRemove.Add(prop.Name);
                        }
                    }

                    foreach (var propToRemove in propertiesToRemove)
                    {
                        o.Remove(propToRemove);
                    }

                    o.WriteTo(writer);
                }
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
            }

            public override bool CanRead
            {
                get { return false; }
            }

            public override bool CanConvert(Type objectType)
            {
                return _types.Any(t => t == objectType);
            }
        }
    }


    public class BaseRepo<TEntity> where TEntity : BaseEntity
    {
        protected DbContext _context;
        private SaveParameters _saveParameters;

        public IQueryable<TEntity> All { get { return _context.Set<TEntity>(); } }

        public BaseRepo(DbContext context, SaveParameters saveParameters)
        {
            _context = context;
            _saveParameters = saveParameters;
            //_dbSet = _context.Set<TEntity>();
        }

        public void Add<T>(T entity) where T : class
        {
            _context.Add(entity);
        }

        public async Task<bool> SaveAll()
        {
            string userIP = _saveParameters.UserIp;
            string userName = _saveParameters.UserName;
            DateTime now = DateTime.Now;

            var modifiedOrAddedEntries = _context.ChangeTracker.Entries()
                 .Where(x =>
                            x.State == EntityState.Added ||
                            x.State == EntityState.Modified
                 ).ToList();

            var logsForNewAddedEntries = new Dictionary<BaseEntity, Log>();

            foreach (var entry in modifiedOrAddedEntries)
            {
                BaseEntity a = (BaseEntity)entry.Entity;

                // Novy i upraveny zaznam
                if (a.GetType() != typeof(Log)) // Log nepotrebuje LastUpdatedX protoze se nikdy nebude upravovat
                {
                    a.LastUpdatedDate = now;
                    a.LastUpdatedIP = userIP;
                    a.LastUpdatedWith = userName;
                }

                // Novy zaznam
                if (entry.State == EntityState.Added)
                {
                    a.CreatedDate = now;
                    a.CreatedIp = userIP;
                    a.CreatedWith = userName;
                }
                
            }

            int changed = await _context.SaveChangesAsync();

            if (changed == 0)
                return false;

            return true;
        }

        public void Delete<T>(T entity) where T : class
        {
            _context.Remove(entity);
        }
    }
}
