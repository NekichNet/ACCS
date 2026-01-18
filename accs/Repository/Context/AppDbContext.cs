using accs.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace accs.Repository.Context
{
	public class AppDbContext : DbContext
	{
		public DbSet<Unit> Units { get; set; }
		public DbSet<Post> Posts { get; set; }
		public DbSet<Rank> Ranks { get; set; }
		public DbSet<Subdivision> Subdivisions { get; set; }
		public DbSet<Permission> Permissions { get; set; }
		public DbSet<Reward> Rewards { get; set; }
		public DbSet<DocType> DocTypes { get; set; }
		public DbSet<Doc> Docs { get; set; }
		public DbSet<Activity> Activities { get; set; }
		public DbSet<UnitStatus> UnitStatuses { get; set; }
		public DbSet<Status> Statuses { get; set; }
		public DbSet<Ticket> Tickets { get; set; }

		public AppDbContext(DbContextOptions<AppDbContext> options)
			: base(options) { }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			/* Разрешения */
			Permission confirmActivity = new Permission { Type = PermissionType.ConfirmActivity, Description = "Подтверждение своей и чужой активности." };
			Permission vacationAccess = new Permission { Type = PermissionType.VacationAccess, Description = "Разрешение на выход в отпуск." };
			Permission giveReprimandGratitude = new Permission { Type = PermissionType.GiveReprimandGratitude, Description = "Возможность выдавать выговора и благодарности нижестоящим бойцам." };
			Permission forceVacation = new Permission { Type = PermissionType.ForceVacation, Description = "Возможность отправлять в отпуск нижестоящих бойцов." };
			Permission changeRanks = new Permission { Type = PermissionType.ChangeRanks, Description = "Возможность повышать и понижать в звании нижестоящих бойцов." };
			Permission changePosts = new Permission { Type = PermissionType.ChangePosts, Description = "Возможность менять должность нижестоящих бойцов." };
			Permission assignRewards = new Permission { Type = PermissionType.AssignRewards, Description = "Возможность присваивать награды у нижестоящим бойцам." };
			Permission manageStructure = new Permission { Type = PermissionType.ManageStructure, Description = "Возможность управлять нижестоящей структурой клана." };
			Permission manageRewards = new Permission { Type = PermissionType.ManageRewards, Description = "Создание и редактирование существующих наград." };
			Permission manageDocTypes = new Permission { Type = PermissionType.ManageDocTypes, Description = "Создание и редактирование шаблонов документов." };
			Permission administrator = new Permission { Type = PermissionType.Administrator, Description = "Все права без ограничений." };


			/* Звания */
			List<Rank> ranks = new List<Rank>()
			{
				new Rank(1, "Рекрут"),
				new Rank(2, "Рядовой"),
				new Rank(3, "Ефрейтор", new HashSet<Permission> { vacationAccess }),
				new Rank(4, "Мл. Сержант"),
				new Rank(5, "Сержант", new HashSet<Permission> { confirmActivity }),
				new Rank(6, "Ст. Сержант"),
				new Rank(7, "Старшина"),
				new Rank(8, "Прапорщик"),
				new Rank(9, "Мл. Лейтенант"),
				new Rank(10, "Лейтенант"),
				new Rank(11, "Ст. Лейтенант"),
				new Rank(12, "Капитан"),
				new Rank(13, "Майор"),
				new Rank(14, "Подполковник"),
				new Rank(15, "Полковник"),
				new Rank(16, "Генерал-Майор"),
				new Rank(17, "Генерал-Лейтенант"),
				new Rank(18, "Генерал-Полковник")
			};
			for (int i = 1; i < 18; i++)
				ranks[i].InsertPrevious(ranks[i - 1]);


			/* Подразделения */
			List<Subdivision> subdivisions = new List<Subdivision>();


			Subdivision communicationService = new Subdivision("COMMUNICATION_SERVICE_ROLE_ID") { Id = subdivisions.Count + 1, Name = "Служба связи" };
			subdivisions.Add(communicationService);

			Subdivision militaryPolice = new Subdivision("MILITARY_POLICE_ROLE_ID") { Id = subdivisions.Count + 1, Name = "Военная Полиция" };
			subdivisions.Add(militaryPolice);

			Subdivision instructors = new Subdivision("INSTRUCTORS_ROLE_ID") { Id = subdivisions.Count + 1, Name = "Инструкторский корпус" };
			subdivisions.Add(instructors);

			Subdivision rota1Commanders = new Subdivision("ROTA1_COMMANDERS_ROLE_ID") { Id = subdivisions.Count + 1, Name = "Командование 1 Роты", };
			subdivisions.Add(rota1Commanders);

			List<Subdivision> platoons = new List<Subdivision>()
			{
				new Subdivision("PLATOON1_ROTA1_ROLE_ID") { Id = subdivisions.Count + 1, Name = "1 Пехотный взвод 1 Роты" },
				new Subdivision("PLATOON2_ROTA1_ROLE_ID") { Id = subdivisions.Count + 2, Name = "2 Взвод СпН 1 Роты" },
				new Subdivision("PLATOON3_ROTA1_ROLE_ID") { Id = subdivisions.Count + 3, Name = "3 Механизированный взвод 1 Роты" }
			};
			subdivisions.AddRange(platoons);


			/* Должности */
			List<Post> posts = new List<Post>();


			Post commander = new Post("COMMANDER_ROLE_ID") { Id = posts.Count + 1, Name = "Командир РХБЗ" };
			posts.Add(commander);

			Post depCommander = new Post("DEPUTY_COMMANDER_ROLE_ID") { Id = posts.Count + 1, Name = "Заместитель командира РХБЗ", Head = commander, Permissions = new HashSet<Permission> { administrator } };
			posts.Add(depCommander);

			Post hqHead = new Post("HQ_HEAD_ROLE_ID") { Id = posts.Count + 1, Name = "Начальник штаба", Head = depCommander };
			posts.Add(hqHead);

			Post depHqHead = new Post("DEPUTY_HQ_HEAD_ROLE_ID") { Id = posts.Count + 1, Name = "Заместитель начальника штаба", Head = hqHead, Permissions = new HashSet<Permission> { changeRanks, manageDocTypes } };
			posts.Add(depHqHead);

			Post rota1Commander = new Post("ROTA_COMMANDER_ROLE_ID") { Id = posts.Count + 1, Name = "Командир Роты", Head = depHqHead, Subdivision = rota1Commanders, Permissions = new HashSet<Permission> { manageStructure } };
			posts.Add(rota1Commander);

			Post zampolit = new Post("ZAMPOLIT_ROLE_ID") { Id = posts.Count + 1, Name = "Замполит", Head = rota1Commander, Subdivision = rota1Commanders, Permissions = new HashSet<Permission> { forceVacation, changePosts } };
			posts.Add(zampolit);


			foreach (Subdivision platoon in platoons)
			{
				Post platoonCommander = new Post("PLATOON_COMMANDER_ROLE_ID") { Id = posts.Count + 1, Name = "Командир взвода", Head = zampolit };
				Post depPlatoonCommander = new Post("DEPUTY_PLATOON_COMMANDER_ROLE_ID") { Id = posts.Count + 2, Name = "Заместитель командира взвода", Head = platoonCommander };
				Post shooter = new Post("SHOOTER_ROLE_ID") { Id = posts.Count + 3, Name = "Стрелок", Head = depPlatoonCommander };
				posts.AddRange(shooter, depPlatoonCommander, platoonCommander);
				platoon.Posts.AddRange(shooter, depPlatoonCommander, platoonCommander);
			}

			/* Виды временных статусов */

			modelBuilder.Entity<Status>().HasData(
				new Status("VACATION_ROLE_ID") { Type = StatusType.Vacation, Name = "Отпуск" },
				new Status("TEMPORARY_POST_ROLE_ID") { Type = StatusType.TemporaryPost, Name = "ВРИД" },
				new Status("GRATITUDE_ROLE_ID") { Type = StatusType.Gratitude, Name = "Благодарность" },
				new Status("REPRIMAND_ROLE_ID") { Type = StatusType.Reprimand, Name = "Выговор" },
				new Status("SEVERE_REPRIMAND_ROLE_ID") { Type = StatusType.SevereReprimand, Name = "Строгий выговор" },
				new Status("RETIREMENT_ROLE_ID") { Type = StatusType.Retirement, Name = "Отставка" }
			);


			modelBuilder.Entity<Permission>().HasData(
				confirmActivity, vacationAccess, giveReprimandGratitude, forceVacation, changeRanks,
				changePosts, assignRewards, manageStructure, manageRewards, manageDocTypes, administrator
			);
			modelBuilder.Entity<Rank>().HasData(ranks);
			modelBuilder.Entity<Subdivision>().HasData(subdivisions);
			modelBuilder.Entity<Post>().HasData(posts);
		}
	}
}
