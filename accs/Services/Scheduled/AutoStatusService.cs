using accs.Database;
using accs.Models;
using accs.Models.Enums;
using accs.Models.Statuses.Abstraction;
using accs.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace accs.Services.Scheduled
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
				int counter = 1;
				while (nextGeneralSession.DayOfWeek != DayOfWeek.Wednesday
					&& nextGeneralSession.DayOfWeek != DayOfWeek.Saturday)
				{
					nextGeneralSession = now.Date.AddDays(counter);
					counter++;
				}
				nextGeneralSession = now.Date.AddDays(counter);

				TimeSpan delay = nextGeneralSession - now;

				await Task.Delay(delay, stoppingToken);

				AppDbContext db = _services.GetRequiredService<AppDbContext>();
				ILogger log = _services.GetRequiredService<ILogger>();

				UnitState? severeReprimand = await db.UnitStates.FindAsync(StatusType.SevereReprimand);
				UnitState? reprimand = await db.UnitStates.FindAsync(StatusType.Reprimand);
				UnitState? gratitude = await db.UnitStates.FindAsync(StatusType.Gratitude);

				if (severeReprimand == null || reprimand == null || gratitude == null)
				{
					Console.WriteLine("AutoStatusService: Ошибка получения статусов благодарностей и выговоров!");
					return;
				}

				log.LogInformation("Старт выдачи благодарностей и выговоров в обязательные сборы");

				foreach (Unit unit in await db.Units.Where(u => !u.HasPermission(PermissionType.AutoReprimandImmune)).ToListAsync())
				{
					if (unit.Activities.Any(a => a.Date == DateOnly.FromDateTime(nextGeneralSession.AddDays(-1)))) // был на обязательных сборах
					{
						UnitStatus? unitStatus = unit.UnitStates.FirstOrDefault(us => !us.IsCompleted()
						&& (us.Status == severeReprimand || us.Status == reprimand || us.Status == gratitude));

						if (unitStatus != null)
						{
							switch (unitStatus.Status.Type)
							{
								case StatusType.SevereReprimand:
									{
										unitStatus.End = DateTime.UtcNow;
										UnitStatus newStatus = new UnitStatus { Start = DateTime.UtcNow, Unit = unit, Status = reprimand };
										db.UnitStatuses.Add(newStatus);
										log.LogInformation($"Бойцу {unit.GetOnlyNickname()} автоматически выдан выговор");
										break;
									}
								case StatusType.Reprimand:
									{
										unitStatus.End = DateTime.UtcNow;
										log.LogInformation($"Бойцу {unit.GetOnlyNickname()} закрыт выговор");
										break;
									}
								case StatusType.Gratitude:
									{
										unitStatus.End = DateTime.UtcNow;
										UnitStatus newStatus = new UnitStatus { Start = DateTime.UtcNow, Unit = unit, Status = gratitude };
										db.UnitStatuses.Add(newStatus);
										log.LogInformation($"Бойцу {unit.GetOnlyNickname()} автоматически выдана благодарность");
										break;
									}
							}
						}
						else
						{
							UnitStatus newStatus = new UnitStatus { Start = DateTime.UtcNow, Unit = unit, Status = gratitude };
							db.UnitStatuses.Add(newStatus);
							log.LogInformation($"Бойцу {unit.GetOnlyNickname()} автоматически выдана благодарность");
						}

						await db.SaveChangesAsync();
					}
					else // не был на обязательных сборах
					{
						if (!unit.UnitStates.Any(us => us.Status.Type == StatusType.Vacation || us.Status.Type == StatusType.Retirement)
							&& unit.Posts.Any())
						{
							UnitStatus? unitStatus = unit.UnitStates.FirstOrDefault(us => !us.IsCompleted()
								&& (us.Status == severeReprimand || us.Status == reprimand || us.Status == gratitude));

							if (unitStatus != null)
							{
								switch (unitStatus.Status.Type)
								{
									case StatusType.SevereReprimand:
										{
											unitStatus.End = DateTime.UtcNow;
											UnitStatus newStatus = new UnitStatus { Start = DateTime.UtcNow, Unit = unit, Status = severeReprimand };
											db.UnitStatuses.Add(newStatus);
											log.LogInformation($"Бойцу {unit.GetOnlyNickname()} автоматически выдан строгий выговор");
											break;
										}
									case StatusType.Reprimand:
										{
											unitStatus.End = DateTime.UtcNow;
											UnitStatus newStatus = new UnitStatus { Start = DateTime.UtcNow, Unit = unit, Status = severeReprimand };
											db.UnitStatuses.Add(newStatus);
											log.LogInformation($"Бойцу {unit.GetOnlyNickname()} автоматически выдана строгий выговор");
											break;
										}
									case StatusType.Gratitude:
										{
											unitStatus.End = DateTime.UtcNow;
											log.LogInformation($"Бойцу {unit.GetOnlyNickname()} автоматически закрыта благодарность");
											break;
										}
								}
							}
							else
							{
								UnitStatus newStatus = new UnitStatus { Start = DateTime.UtcNow, Unit = unit, Status = reprimand };
								db.UnitStatuses.Add(newStatus);
								log.LogInformation($"Бойцу {unit.GetOnlyNickname()} автоматически выдан выговор");
							}

							await db.SaveChangesAsync();
						}
					}
				}

				log.LogInformation("Конец выдачи благодарностей и выговоров в обязательные сборы");
			}
		}
    }
}
