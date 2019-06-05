using Contoso.Events.Data;
using Contoso.Events.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Contoso.Events.Worker
{
    public static class ProcessDocuments
    {
        private static ConnectionManager _connection = new ConnectionManager();
        private static RegistrationContext _registrationsContext = _connection.GetCosmosContext();

        [FunctionName("ProcessDocuments")]
        //public static async Task Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")]HttpRequest request, TraceWriter log)
        //public static async Task Run([BlobTrigger("signinsheets-pending/{name}")] Stream input, string name, Stream output, TraceWriter log)
        public static async Task Run([BlobTrigger("signinsheets-pending/{name}")] Stream input, string name, [Blob("signinsheets/{name}", FileAccess.Write)] Stream output, TraceWriter log)
        {
            //string message = request.Query["eventkey"];
            string eventKey = Path.GetFileNameWithoutExtension(name);

            //using (MemoryStream stream = await ProcessStorageMessage(eventKey))
            //{
            //    byte[] byteArray = stream.ToArray();
            //    await output.WriteAsync(byteArray, 0, byteArray.Length);
            //}

            log.Info($"Request received to generate sign-in sheet for event: {eventKey}");

            var registrants = await ProcessHttpRequestMessage(eventKey);

            log.Info($"Registrants: {String.Join(", ", registrants)}");

            log.Info($"Request completed for event: {eventKey}");
        }

        private static Task<MemoryStream> ProcessStorageMessage(string eventKey)
        {
            throw new NotImplementedException();
        }

        private static async Task<List<string>> ProcessHttpRequestMessage(string eventKey)
        {
            using (EventsContext eventsContext = _connection.GetSqlContext())
            {
                await eventsContext.Database.EnsureCreatedAsync();
                await _registrationsContext.ConfigureConnectionAsync();
                List<string> registrants = await _registrationsContext.GetRegistrantsForEvent(eventKey);
                Event eventEntry = await eventsContext.Events.SingleOrDefaultAsync(e => e.EventKey == eventKey);

                return registrants;
            }
        }
    }
}
