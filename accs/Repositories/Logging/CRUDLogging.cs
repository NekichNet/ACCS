using accs.Logging;

namespace accs.Repositories.Logging
{
    public static class CRUDLogging<T> where T : notnull
	{
        public static void LogCreated(ILogger log, T entity)
            => log.LogTrace(eventId: EventIds.Created,
                "Entity created: " + entity.ToString());

		public static void LogRead(ILogger log, T entity)
			=> log.LogTrace(eventId: EventIds.Read,
				"Entity read: " + entity.ToString());

		public static void LogUpdated(ILogger log, T entity)
			=> log.LogTrace(eventId: EventIds.Updated,
				"Entity updated: " + entity.ToString());

		public static void LogDeleted(ILogger log, T entity)
			=> log.LogTrace(eventId: EventIds.Deleted,
				"Entity deleted: " + entity.ToString());

		public static void LogNotFound(ILogger log, T entity)
			=> log.LogTrace(eventId: EventIds.NotFound,
				"Entity not found: " + entity.ToString());

		public static void LogNoData(ILogger log, T entity)
			=> log.LogTrace(eventId: EventIds.NoData,
				"No data of entity: " + entity.ToString());
	}
}
