namespace accs.Models.Database
{
    public class DiscordNotification
    {
        public int Id { get; set; }
		public ulong ChannelId { get; set; }
		public string Shortened { get; set; } = string.Empty;
		public string Text { get; set; } = string.Empty;
		public string Footer { get; set; } = string.Empty;
        public string Images { get; set; } = string.Empty;
		public ulong? AuthorId { get; set; }
        public string EmbedColor { get; set; } = string.Empty;

		public DiscordNotification() { }

		public void SetEmbedColor(Discord.Color color)
		{
			EmbedColor = color.ToString();
		}

		public Discord.Color GetEmbedColor()
		{
			return Discord.Color.Parse(EmbedColor);
		}

		public DiscordNotification ApplyReplace(Dictionary<string, string> replaces)
		{
			DiscordNotification notification = new DiscordNotification();

			notification.Id = Id;
			notification.ChannelId = ChannelId;
			notification.Shortened = Shortened;
			notification.Text = Text;
			notification.Footer = Footer;
			notification.Images = Images;
			notification.AuthorId = AuthorId;
			notification.EmbedColor = EmbedColor;

			foreach (KeyValuePair<string, string> pair in replaces)
			{
				notification.Shortened = notification.Shortened.Replace(pair.Key, pair.Value);
				notification.Text = notification.Text.Replace(pair.Key, pair.Value);
				notification.Footer = notification.Footer.Replace(pair.Key, pair.Value);
			}

			return notification;
		}

        public override string ToString()
        {
            return Id.ToString();
        }
	}
}
