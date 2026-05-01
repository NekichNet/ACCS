using accs.Database;
using accs.Models;
using accs.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Quartz;

//namespace accs.Services
//{
//    public class AutoStatusService : IJob
//    {
//		private readonly AppDbContext _db;
//		private readonly ILogger _log;

//        public AutoStatusService(AppDbContext db, ILogger log)
//        {
//            _db = db;
//			_log = log;
//        }

//		public async Task Execute(IJobExecutionContext context)
//		{
//			Status? severeReprimand = await _db.Statuses.FindAsync(StatusType.SevereReprimand);
//			Status? reprimand = await _db.Statuses.FindAsync(StatusType.Reprimand);
//			Status? gratitude = await _db.Statuses.FindAsync(StatusType.Gratitude);

//			if (severeReprimand == null || reprimand == null || gratitude == null)
//			{
//				Console.WriteLine("AutoStatusService: Ошибка получения статусов благодарностей и выговоров!");
//				return;
//			}

//			_log.LogInformation("Старт выдачи благодарностей и выговоров в обязательные сборы");

//			foreach (Unit unit in await _db.Units
//				.Where(u => u.Posts.Any())
//				.Where(u => !u.HasPermission(PermissionType.AutoReprimandImmune)).ToListAsync())
//			{
//				if (unit.Activities.Any(a => a.Date == DateOnly.FromDateTime(DateTime.Today.AddDays(-1)))) // был на обязательных сборах
//				{
//					UnitStatus? unitStatus = unit.UnitStatuses.FirstOrDefault(us => !us.IsCompleted()
//					&& (us.Status == severeReprimand || us.Status == reprimand || us.Status == gratitude));

//					if (unitStatus != null)
//					{
//						switch (unitStatus.Status.Type)
//						{
//							case StatusType.SevereReprimand:
//								{
//									unitStatus.EndDate = DateTime.UtcNow;
//									UnitStatus newStatus = new UnitStatus { StartDate = DateTime.UtcNow, Unit = unit, Status = reprimand };
//									_db.UnitStatuses.Add(newStatus);
//									_log.LogInformation($"Бойцу {unit.GetOnlyNickname()} автоматически выдан выговор");
//									break;
//								}
//							case StatusType.Reprimand:
//								{
//									unitStatus.EndDate = DateTime.UtcNow;
//									_log.LogInformation($"Бойцу {unit.GetOnlyNickname()} закрыт выговор");
//									break;
//								}
//							case StatusType.Gratitude:
//								{
//									unitStatus.EndDate = DateTime.UtcNow;
//									UnitStatus newStatus = new UnitStatus { StartDate = DateTime.UtcNow, Unit = unit, Status = gratitude };
//									_db.UnitStatuses.Add(newStatus);
//									_log.LogInformation($"Бойцу {unit.GetOnlyNickname()} автоматически выдана благодарность");
//									break;
//								}
//						}
//					}
//					else
//					{
//						UnitStatus newStatus = new UnitStatus { StartDate = DateTime.UtcNow, Unit = unit, Status = gratitude };
//						_db.UnitStatuses.Add(newStatus);
//						_log.LogInformation($"Бойцу {unit.GetOnlyNickname()} автоматически выдана благодарность");
//					}

//					await _db.SaveChangesAsync();
//				}
//				else // не был на обязательных сборах
//				{
//					if (!unit.UnitStatuses.Any(us => us.Status.Type == StatusType.Vacation || us.Status.Type == StatusType.Retirement)
//						&& unit.Posts.Any())
//					{
//						UnitStatus? unitStatus = unit.UnitStatuses.FirstOrDefault(us => !us.IsCompleted()
//							&& (us.Status == severeReprimand || us.Status == reprimand || us.Status == gratitude));

//						if (unitStatus != null)
//						{
//							switch (unitStatus.Status.Type)
//							{
//								case StatusType.SevereReprimand:
//									{
//										unitStatus.EndDate = DateTime.UtcNow;
//										UnitStatus newStatus = new UnitStatus { StartDate = DateTime.UtcNow, Unit = unit, Status = severeReprimand };
//										_db.UnitStatuses.Add(newStatus);
//										_log.LogInformation($"Бойцу {unit.GetOnlyNickname()} автоматически выдан строгий выговор");
//										break;
//									}
//								case StatusType.Reprimand:
//									{
//										unitStatus.EndDate = DateTime.UtcNow;
//										UnitStatus newStatus = new UnitStatus { StartDate = DateTime.UtcNow, Unit = unit, Status = severeReprimand };
//										_db.UnitStatuses.Add(newStatus);
//										_log.LogInformation($"Бойцу {unit.GetOnlyNickname()} автоматически выдана строгий выговор");
//										break;
//									}
//								case StatusType.Gratitude:
//									{
//										unitStatus.EndDate = DateTime.UtcNow;
//										_log.LogInformation($"Бойцу {unit.GetOnlyNickname()} автоматически закрыта благодарность");
//										break;
//									}
//							}
//						}
//						else
//						{
//							UnitStatus newStatus = new UnitStatus { StartDate = DateTime.UtcNow, Unit = unit, Status = reprimand };
//							_db.UnitStatuses.Add(newStatus);
//							_log.LogInformation($"Бойцу {unit.GetOnlyNickname()} автоматически выдан выговор");
//						}

//						await _db.SaveChangesAsync();
//					}
//				}
//			}

//			_log.LogInformation("Конец выдачи благодарностей и выговоров в обязательные сборы");
//		}
//    }
//}
