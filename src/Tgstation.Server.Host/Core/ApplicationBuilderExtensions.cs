﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Net;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Extensions for <see cref="IApplicationBuilder"/>
	/// </summary>
	static class ApplicationBuilderExtensions
	{
		/// <summary>
		/// Gets a <see cref="ILogger"/> from a given <paramref name="httpContext"/>
		/// </summary>
		/// <param name="httpContext">The <see cref="HttpContext"/> to get the <see cref="ILogger"/> from</param>
		/// <returns>A new <see cref="ILogger"/></returns>
		static ILogger GetLogger(HttpContext httpContext) => httpContext.RequestServices.GetRequiredService<ILogger<Application>>();

		/// <summary>
		/// Return a <see cref="ConflictObjectResult"/> for <see cref="DbUpdateException"/>s
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure</param>
		public static void UseDbConflictHandling(this IApplicationBuilder applicationBuilder)
		{
			if (applicationBuilder == null)
				throw new ArgumentNullException(nameof(applicationBuilder));
			applicationBuilder.Use(async (context, next) =>
			{
				var logger = GetLogger(context);
				try
				{
					await next().ConfigureAwait(false);
				}
				catch (DbUpdateException e)
				{
					logger.LogDebug("Database conflict: {0}", e.Message);
					await new ConflictObjectResult(new ErrorMessage { Message = String.Format(CultureInfo.InvariantCulture, "A database conflict has occurred: {0}", (e.InnerException ?? e).Message) }).ExecuteResultAsync(new ActionContext
					{
						HttpContext = context
					}).ConfigureAwait(false);
				}
			});
		}

		/// <summary>
		/// Suppress TaskCanceledException warnings when a user aborts a request
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure</param>
		public static void UseCancelledRequestSuppression(this IApplicationBuilder applicationBuilder)
		{
			if (applicationBuilder == null)
				throw new ArgumentNullException(nameof(applicationBuilder));
			applicationBuilder.Use(async (context, next) =>
			{
				var logger = GetLogger(context);
				try
				{
					await next().ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					logger.LogDebug("Request cancelled!");
				}
			});
		}

		/// <summary>
		/// Suppress all in flight exceptions with error 500.
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure</param>
		public static void UseServerErrorHandling(this IApplicationBuilder applicationBuilder)
		{
			if (applicationBuilder == null)
				throw new ArgumentNullException(nameof(applicationBuilder));
			applicationBuilder.Use(async (context, next) =>
			{
				var logger = GetLogger(context);
				try
				{
					await next().ConfigureAwait(false);
				}
				catch (Exception e)
				{
					logger.LogError("Failed request: {0}", e);
					await new ObjectResult(
						new ErrorMessage
						{
							Message = $"A unhandled exception has occurred: {e}"
						})
					{
						StatusCode = (int)HttpStatusCode.InternalServerError
					}
					.ExecuteResultAsync(new ActionContext
					{
						HttpContext = context
					}).ConfigureAwait(false);
				}
			});
		}
	}
}
