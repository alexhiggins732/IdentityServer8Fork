/*
 Copyright (c) 2024 HigginsSoft
 Written by Alexander Higgins https://github.com/alexhiggins732/ 
 

 Copyright (c) 2018, Brock Allen & Dominick Baier. All rights reserved.

 Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information. 
 Source code for this software can be found at https://github.com/alexhiggins732/IdentityServer8

 The above copyright notice and this permission notice shall be included in all
 copies or substantial portions of the Software.

*/

using System;
using System.Threading.Tasks;
using IdentityServer8.Models;
using IdentityServer8.Extensions;
using IdentityServer8.Validation;
using Microsoft.Extensions.Logging;
using IdentityServer8.Stores;
using System.Collections.Specialized;

namespace IdentityServer8.Services
{
    internal class OidcReturnUrlParser : IReturnUrlParser
    {
        private readonly IAuthorizeRequestValidator _validator;
        private readonly IUserSession _userSession;
        private readonly ILogger _logger;
        private readonly IAuthorizationParametersMessageStore _authorizationParametersMessageStore;

        public OidcReturnUrlParser(
            IAuthorizeRequestValidator validator,
            IUserSession userSession,
            ILogger<OidcReturnUrlParser> logger,
            IAuthorizationParametersMessageStore authorizationParametersMessageStore = null)
        {
            _validator = validator;
            _userSession = userSession;
            _logger = logger;
            _authorizationParametersMessageStore = authorizationParametersMessageStore;
        }

        public async Task<AuthorizationRequest> ParseAsync(string returnUrl)
        {
            if (IsValidReturnUrl(returnUrl))
            {
                var parameters = returnUrl.ReadQueryStringAsNameValueCollection();
                if (_authorizationParametersMessageStore != null)
                {
                    var messageStoreId = parameters[Constants.AuthorizationParamsStore.MessageStoreIdParameterName];
                    var entry = await _authorizationParametersMessageStore.ReadAsync(messageStoreId);
                    parameters = entry?.Data.FromFullDictionary() ?? new NameValueCollection();
                }

                var user = await _userSession.GetUserAsync();
                var result = await _validator.ValidateAsync(parameters, user);
                if (!result.IsError)
                {
                    _logger.LogTrace("AuthorizationRequest being returned");
                    return new AuthorizationRequest(result.ValidatedRequest);
                }
            }

            _logger.LogTrace("No AuthorizationRequest being returned");
            return null;
        }

        public bool IsValidReturnUrl(string returnUrl)
        {
            if (returnUrl.IsLocalUrl())
            {
                var index = returnUrl.IndexOf('?');
                if (index >= 0)
                {
                    returnUrl = returnUrl.Substring(0, index);
                }

                if (returnUrl.EndsWith(Constants.ProtocolRoutePaths.Authorize, StringComparison.Ordinal) ||
                    returnUrl.EndsWith(Constants.ProtocolRoutePaths.AuthorizeCallback, StringComparison.Ordinal))
                {
                    _logger.LogTrace("returnUrl is valid");
                    return true;
                }
            }

            _logger.LogTrace("returnUrl is not valid");
            return false;
        }
    }
}
