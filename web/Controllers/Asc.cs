using System.Security.Authentication;
using Atlas_Web.Authorization;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Atlas_Web.Controllers
{
    [AllowAnonymous]
    [Route("Auth/Asc")]
    public class AscController : Controller
    {
        private readonly Saml2Configuration config;
        const string relayStateReturnUrl = "ReturnUrl";

        public AscController(Saml2Configuration config)
        {
            this.config = config;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var binding = new Saml2PostBinding();
            var saml2AuthnResponse = new Saml2AuthnResponse(config);
            var relayStateQuery = binding.GetRelayStateQuery();
            var returnUrl = relayStateQuery.ContainsKey(relayStateReturnUrl)
                ? relayStateQuery[relayStateReturnUrl]
                : Url.Content("~/");

            // if a login existed.. use it
            if (User.Identity.IsAuthenticated)
            {
                return Redirect(returnUrl);
            }

            binding.ReadSamlResponse(Request.ToGenericHttpRequest(), saml2AuthnResponse);
            if (saml2AuthnResponse.Status != Saml2StatusCodes.Success)
            {
                throw new AuthenticationException(
                    $"SAML Response status: {saml2AuthnResponse.Status}"
                );
            }

            binding.Unbind(Request.ToGenericHttpRequest(), saml2AuthnResponse);

            await saml2AuthnResponse.CreateSession(
                HttpContext,
                claimsTransform: (claimsPrincipal) => ClaimsTransform.Transform(claimsPrincipal)
            );

            return Redirect(returnUrl);
        }
    }
}
