using accs.Database;
using accs.Models;
using accs.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace accs.Services
{
    public class AutoStatusService : BackgroundService
    {
		private readonly IServiceProvider _services;

        public AutoStatusService(IServiceProvider services)
        {
            _services = services;
        }

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
			while (!stoppingToken.IsCancellationRequested)
			{
				DateTime now = DateTime.Now;
				DateTime nextGeneralSession = now.Date;
				while (nextGeneralSession.DayOfWeek != DayOfWeek.Wednesday
					|| nextGeneralSession.DayOfWeek != DayOfWeek.Saturday)
					nextGeneralSession.AddDays(1);
				nextGeneralSession.AddDays(1);

				TimeSpan delay = nextGeneralSession - now;

				await Task.Delay(delay, stoppingToken);

				using (var scope = _services.CreateScope())
				{
					AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

					Status? severeReprimand = await db.Statuses.FindAsync(StatusType.SevereReprimand);
					Status? reprimand = await db.Statuses.FindAsync(StatusType.Reprimand);
					Status? gratitude = await db.Statuses.FindAsync(StatusType.Gratitude);

					if (severeReprimand == null || reprimand == null || gratitude == null)
					{
						Console.WriteLine("AutoStatusService: Ошибка получения статусов благодарностей и выговоров!");
						return;
					}

					foreach (Unit unit in await db.Units.Where(u => !u.HasPermission(PermissionType.AutoReprimandImmune)).ToListAsync())
					{
						if (unit.Activities.Any(a => a.Date == DateOnly.FromDateTime(nextGeneralSession.AddDays(-1)))) // был на обязательных сборах
						{
							UnitStatus? unitStatus = unit.UnitStatuses.FirstOrDefault(us => !us.IsCompleted()
							&& (us.Status == severeReprimand || us.Status == reprimand || us.Status == gratitude));

							if (unitStatus != null)
							{
								switch (unitStatus.Status.Type)
								{
									case StatusType.SevereReprimand:
										{
											unitStatus.EndDate = DateTime.UtcNow;
											UnitStatus newStatus = new UnitStatus { StartDate = DateTime.UtcNow, Unit = unit, Status = reprimand };
											db.Add(newStatus);
											break;
										}
									case StatusType.Reprimand:
										{
											unitStatus.EndDate = DateTime.UtcNow;
											break;
										}
									case StatusType.Gratitude:
										{
											unitStatus.EndDate = DateTime.UtcNow;
											UnitStatus newStatus = new UnitStatus { StartDate = DateTime.UtcNow, Unit = unit, Status = gratitude };
											db.Add(newStatus);
											break;
										}
								}
							}
							else
							{
								UnitStatus newStatus = new UnitStatus { StartDate = DateTime.UtcNow, Unit = unit, Status = gratitude };
								db.Add(newStatus);
							}

							await db.SaveChangesAsync();
						}
						else // не был на обязательных сборах
						{
							if (!unit.UnitStatuses.Any(us => us.Status.Type == StatusType.Vacation || us.Status.Type == StatusType.Retirement)
								&& unit.Posts.Any())
							{
								UnitStatus? unitStatus = unit.UnitStatuses.FirstOrDefault(us => !us.IsCompleted()
							&& (us.Status == severeReprimand || us.Status == reprimand || us.Status == gratitude));

								if (unitStatus != null)
								{
									switch (unitStatus.Status.Type)
									{
										case StatusType.SevereReprimand:
											{
												unitStatus.EndDate = DateTime.UtcNow;
												UnitStatus newStatus = new UnitStatus { StartDate = DateTime.UtcNow, Unit = unit, Status = severeReprimand };
												db.Add(newStatus);
												break;
											}
										case StatusType.Reprimand:
											{
												unitStatus.EndDate = DateTime.UtcNow;
												UnitStatus newStatus = new UnitStatus { StartDate = DateTime.UtcNow, Unit = unit, Status = severeReprimand };
												db.Add(newStatus);
												break;
											}
										case StatusType.Gratitude:
											{
												unitStatus.EndDate = DateTime.UtcNow;
												break;
											}
									}
								}
								else
								{
									UnitStatus newStatus = new UnitStatus { StartDate = DateTime.UtcNow, Unit = unit, Status = reprimand };
									db.Add(newStatus);
								}

								await db.SaveChangesAsync();
							}
						}
					}
				}
			}
		}
    }
}
