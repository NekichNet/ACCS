using accs.Models;
using accs.Models.Enums;
using accs.Models.SingleDayEvents;
using accs.Models.SingleDayEvents.Abstraction;
using accs.Models.States;
using accs.Models.States.Abstraction;
using accs.Models.States.Statuses;
using accs.Models.Statuses;
using accs.Models.Statuses.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace accs.Database
{
	public class AppDbContext : DbContext
	{
		public DbSet<Unit> Units { get; set; }
		public DbSet<Post> Posts { get; set; }
		public DbSet<Rank> Ranks { get; set; }
		public DbSet<Subdivision> Subdivisions { get; set; }

		public DbSet<Permission> Permissions { get; set; }
		public DbSet<GivedPermission> GivedPermissions { get; set; }

		public DbSet<Reward> Rewards { get; set; }
		public DbSet<AssignedReward> AssignedRewards { get; set; }

		public DbSet<Doc> Docs { get; set; }
		public DbSet<Activity> Activities { get; set; }

		public DbSet<UnitState> UnitStates { get; set; }
		public DbSet<StateWithDoc> StatesWithDoc { get; set; }
		public DbSet<Status> Statuses { get; set; }
		public DbSet<Gratitude> Gratitudes { get; set; }
		public DbSet<NoStatus> NoStatuses { get; set; }
		public DbSet<Reprimand> Reprimands { get; set; }
		public DbSet<SevereReprimand> SevereReprimands { get; set; }
		public DbSet<AssignedPost> AssignedPosts { get; set; }
		public DbSet<AssignedRank> AssignedRanks { get; set; }
		public DbSet<Retirement> Retirements { get; set; }

		public DbSet<SingleDayEvent> SingleDayEvents { get; set; }
		public DbSet<EventWithDoc> EventsWithDoc { get; set; }
		public DbSet<CustomEvent> CustomEvents { get; set; }
		public DbSet<CustomEventWithDoc> CustomEventsWithDoc { get; set; }
		public DbSet<RewardAssignmentEvent> RewardAssignmentEvents { get; set; }
		public DbSet<UnitRegistrationEvent> UnitRegistrationEvents { get; set; }
		public DbSet<UnitDismissingEvent> UnitDismissingEvents { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
			: base(options) { }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder
				.UseLazyLoadingProxies();
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			/* Разрешения */
			Permission confirmActivity = new Permission { Type = PermissionType.ConfirmActivity, Name = "Подтверждение активности", Description = "Подтверждение своей и чужой активности." };
			Permission vacationAccess = new Permission { Type = PermissionType.VacationAccess, Name = "Выход в отпуск", Description = "Разрешение на выход в отпуск." };
			Permission giveReprimandGratitude = new Permission { Type = PermissionType.GiveReprimandGratitude, Name = "Выдача выговоров/благодарностей", Description = "Возможность выдавать выговора и благодарности нижестоящим бойцам." };
			Permission forceVacation = new Permission { Type = PermissionType.ForceVacation, Name = "Отправка других в отпуск", Description = "Возможность отправлять в отпуск нижестоящих бойцов." };
			Permission changeRanks = new Permission { Type = PermissionType.AssignRanks, Name = "Присваивание званий", Description = "Возможность повышать и понижать в звании нижестоящих бойцов." };
			Permission changePosts = new Permission { Type = PermissionType.AssignPosts, Name = "Назначение на должности", Description = "Возможность менять должность нижестоящих бойцов." };
			Permission assignRewards = new Permission { Type = PermissionType.AssignRewards, Name = "Присваивание наград", Description = "Возможность присваивать награды у нижестоящим бойцам." };
			Permission manageStructure = new Permission { Type = PermissionType.ManageStructure, Name = "Управление структурой", Description = "Возможность управлять нижестоящей структурой клана." };
			Permission manageRewards = new Permission { Type = PermissionType.ManageRewards, Name = "Управление наградами", Description = "Создание и редактирование существующих наград." };
			Permission manageDocTypes = new Permission { Type = PermissionType.ManageDocTypes, Name = "Управление шаблонами документов", Description = "Создание и редактирование шаблонов документов." };
			Permission administrator = new Permission { Type = PermissionType.Administrator, Name = "Администратор", Description = "Все права без ограничений." };
			Permission moderateNicknames = new Permission { Type = PermissionType.ModerateNicknames, Name = "Изменение чужих никнеймов", Description = "Право изменять чужие никнеймы." };
			Permission autoReprimandImmune = new Permission { Type = PermissionType.AutoReprimandImmune, Name = "Освобождение от сборов", Description = "Иммунитет к автоматической выдаче выговора за отстутствие на сборах." };

			modelBuilder.Entity<Permission>().HasData(
				confirmActivity, vacationAccess, giveReprimandGratitude, forceVacation, changeRanks,
				changePosts, assignRewards, manageStructure, manageRewards, manageDocTypes, administrator,
				autoReprimandImmune
			);

			/* Звания */
			List<Rank> ranks = new List<Rank>()
			{
				new Rank(1, "Рекрут"),
				new Rank(2, "Рядовой"),
				new Rank(3, "Ефрейтор"),
				new Rank(4, "Мл. Сержант"),
				new Rank(5, "Сержант"),
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

			modelBuilder.Entity<UnitState>().HasData(
				new UnitState() { Type = StatusType.Vacation, Name = "Отпуск" },
				new UnitState() { Type = StatusType.TemporaryPost, Name = "ВрИО" },
				new UnitState() { Type = StatusType.Gratitude, Name = "Благодарность" },
				new UnitState() { Type = StatusType.Reprimand, Name = "Выговор" },
				new UnitState() { Type = StatusType.SevereReprimand, Name = "Строгий выговор" },
				new UnitState() { Type = StatusType.Retirement, Name = "Отставка" }
			);
		}
	}
}
