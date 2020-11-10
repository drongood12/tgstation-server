using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// kek
	/// </summary>
	sealed class KekCommand : ICommand
	{
		/// <summary>
		/// kek
		/// </summary>
		const string Kek = "Оборжатся можно блять";

		/// <inheritdoc />
		public string Name => "kek";

		/// <inheritdoc />
		public string HelpText => "Оборжатся можно блять";

		/// <inheritdoc />
		public bool AdminOnly => false;

		/// <inheritdoc />
		public Task<string> Invoke(string arguments, ChatUser user, CancellationToken cancellationToken) => Task.FromResult(Kek);
	}
}
