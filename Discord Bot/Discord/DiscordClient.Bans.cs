using System;
using System.Net;
using System.Threading.Tasks;

namespace Discord
{
	public class BanEventArgs : EventArgs
	{
		public string UserId { get; }
		public Server Server { get; }

		internal BanEventArgs(string userId, Server server)
		{
			UserId = userId;
			Server = server;
		}
	}

	public partial class DiscordClient
	{
		public event EventHandler<BanEventArgs> UserBanned;
		private void RaiseUserBanned(string userId, Server server)
		{
			if (UserBanned != null)
				RaiseEvent(nameof(UserBanned), () => UserBanned(this, new BanEventArgs(userId, server)));
		}
		public event EventHandler<BanEventArgs> UserUnbanned;
		private void RaiseUserUnbanned(string userId, Server server)
		{
			if (UserUnbanned != null)
				RaiseEvent(nameof(UserUnbanned), () => UserUnbanned(this, new BanEventArgs(userId, server)));
		}

		/// <summary> Bans a user from the provided server. </summary>
		public Task Ban(User user)
		{
			if (user == null) throw new ArgumentNullException(nameof(user));
			if (user.Server == null) throw new ArgumentException("Unable to ban a user in a private chat.");
			CheckReady();

			return _api.BanUser(user.Server.Id, user.Id);
		}

		/// <summary> Unbans a user from the provided server. </summary>
		public async Task Unban(Server server, string userId)
		{
			if (server == null) throw new ArgumentNullException(nameof(server));
			if (userId == null) throw new ArgumentNullException(nameof(userId));
			CheckReady();

			try { await _api.UnbanUser(server.Id, userId).ConfigureAwait(false); }
			catch (HttpException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }
		}
	}
}