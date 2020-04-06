﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Core.Features.Routing;
using Microsoft.Health.Fhir.SqlServer.Api.Features.Filters;
using Microsoft.Health.Fhir.SqlServer.Api.Features.Routing;
using Microsoft.Health.Fhir.SqlServer.Features.Schema;
using Microsoft.Health.Fhir.SqlServer.Features.Schema.Extensions;

namespace Microsoft.Health.Fhir.SqlServer.Api.Controllers
{
    [HttpExceptionFilter]
    [Route(KnownRoutes.SchemaRoot)]
    public class SchemaController : Controller
    {
        private readonly SchemaInformation _schemaInformation;
        private readonly IUrlResolver _urlResolver;
        private readonly ILogger<SchemaController> _logger;
        private readonly IMediator _mediator;

        public SchemaController(SchemaInformation schemaInformation, IUrlResolver urlResolver, IMediator mediator, ILogger<SchemaController> logger)
        {
            EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));
            EnsureArg.IsNotNull(urlResolver, nameof(urlResolver));
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _schemaInformation = schemaInformation;
            _urlResolver = urlResolver;
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        [Route(KnownRoutes.Versions)]
        public ActionResult AvailableVersions()
        {
            _logger.LogInformation("Attempting to get available schemas");

            var availableSchemas = new List<object>();
            var currentVersion = _schemaInformation.Current ?? 0;
            foreach (var version in Enum.GetValues(typeof(SchemaVersion)).Cast<SchemaVersion>().Where(sv => sv >= currentVersion))
            {
                var routeValues = new Dictionary<string, object> { { "id", (int)version } };
                Uri scriptUri = _urlResolver.ResolveRouteNameUrl(RouteNames.Script, routeValues);
                availableSchemas.Add(new { id = version, script = scriptUri });
            }

            return new JsonResult(availableSchemas);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route(KnownRoutes.Current)]
        public async Task<IActionResult> CurrentVersion()
        {
            _logger.LogInformation("Attempting to get current schemas");

            var compatibleResponse = await _mediator.GetCurrentVersionAsync(HttpContext.RequestAborted);

            return new JsonResult(compatibleResponse.CurrentVersions);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route(KnownRoutes.Script, Name = RouteNames.Script)]
        public FileContentResult SqlScript(int id)
        {
            _logger.LogInformation($"Attem" +
                $"pting to get script for schema version: {id}");
            string fileName = $"{id}.sql";
            return File(ScriptProvider.GetMigrationScriptAsBytes(id), "application/json", fileName);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route(KnownRoutes.Compatibility)]
        public async Task<IActionResult> Compatibility()
        {
            _logger.LogInformation("Attempting to get compatibility");

            var compatibleResponse = await _mediator.GetCompatibleVersionAsync(HttpContext.RequestAborted);

            return new JsonResult(compatibleResponse.Versions);
        }
    }
}