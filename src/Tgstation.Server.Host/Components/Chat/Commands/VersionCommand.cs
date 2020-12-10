using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// <see cref="ICommand"/> to return the <see cref="IAssemblyInformationProvider.VersionString"/>
	/// </summary>
	sealed class VersionCommand : ICommand
	{
		/// <inheritdoc />
		public string Name => "version";

		/// <inheritdoc />
		public string HelpText => "Отображает версию сервера tgstation";

		/// <inheritdoc />
		public bool AdminOnly => false;

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="VersionCommand"/>
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// Construct a <see cref="VersionCommand"/>
		/// </summary>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/></param>
		public VersionCommand(IAssemblyInformationProvider assemblyInformationProvider)
		{
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
		}

		/// <inheritdoc />
		public Task<string> Invoke(string arguments, ChatUser user, CancellationToken cancellationToken) => Task.FromResult(assemblyInformationProvider.VersionString);
	}
}
