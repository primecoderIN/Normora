<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('username','password') displayInfo=realm.password && realm.registrationAllowed && !registrationDisabled??; section>
    <#if section = "header">
        ${msg("loginAccountTitle")}
    <#elseif section = "form">
    <div id="kc-form">
      <div id="kc-form-wrapper" class="w-full max-w-md mx-auto">
        <#if realm.password>
            <form id="kc-form-login" onsubmit="login.disabled = true; return true;" action="${url.loginAction}" method="post" class="space-y-6">
                
                <#if !usernameHidden??>
                    <div>
                        <label for="username" class="block text-sm font-medium text-gray-700">
                            <#if !realm.loginWithEmailAllowed>${msg("username")}<#elseif !realm.registrationEmailAsUsername>${msg("usernameOrEmail")}<#else>${msg("email")}</#if>
                        </label>
                        <div class="mt-1">
                            <input tabindex="1" id="username" class="normora-input appearance-none block w-full px-3 py-2 border rounded-md shadow-sm placeholder-gray-400 focus:outline-none sm:text-sm" name="username" value="${(login.username!'')}" type="text" autofocus autocomplete="off"
                                aria-invalid="<#if messagesPerField.existsError('username','password')>true</#if>" />
                        </div>
                        <#if messagesPerField.existsError('username','password')>
                            <span id="input-error" class="text-sm text-red-600 mt-1 block" aria-live="polite">
                                    ${kcSanitize(messagesPerField.getFirstError('username','password'))?no_esc}
                            </span>
                        </#if>
                    </div>
                </#if>

                <div>
                    <div class="flex items-center justify-between">
                        <label for="password" class="block text-sm font-medium text-gray-700">${msg("password")}</label>
                        <#if realm.resetPasswordAllowed>
                            <div class="text-sm">
                                <a tabindex="5" href="${url.loginResetCredentialsUrl}" class="font-medium text-indigo-600 hover:text-indigo-500">${msg("doForgotPassword")}</a>
                            </div>
                        </#if>
                    </div>
                    <div class="mt-1 relative">
                        <input tabindex="2" id="password" class="normora-input appearance-none block w-full px-3 py-2 border rounded-md shadow-sm placeholder-gray-400 focus:outline-none sm:text-sm" name="password" type="password" autocomplete="off"
                            aria-invalid="<#if messagesPerField.existsError('username','password')>true</#if>" />
                    </div>
                </div>

                <div class="flex items-center justify-between">
                    <#if realm.rememberMe && !usernameHidden??>
                        <div class="flex items-center">
                            <input tabindex="3" id="rememberMe" name="rememberMe" type="checkbox" class="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-gray-300 rounded"
                                <#if login.rememberMe??>checked</#if>>
                            <label for="rememberMe" class="ml-2 block text-sm text-gray-900">
                                ${msg("rememberMe")}
                            </label>
                        </div>
                    </#if>
                </div>

                <div>
                    <input type="hidden" id="id-hidden-input" name="credentialId" <#if auth.selectedCredential?has_content>value="${auth.selectedCredential}"</#if>/>
                    <button tabindex="4" class="normora-btn-primary w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-bold focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500" name="login" id="kc-login" type="submit">
                        ${msg("doLogIn")}
                    </button>
                </div>
            </form>
        </#if>
        </div>
      </div>
    <#elseif section = "info" >
        <#if realm.password && realm.registrationAllowed && !registrationDisabled??>
            <div id="kc-registration" class="mt-6 text-center text-sm text-gray-600">
                <span>${msg("noAccount")} <a tabindex="6" href="${url.registrationUrl}" class="font-medium text-indigo-600 hover:text-indigo-500">${msg("doRegister")}</a></span>
            </div>
        </#if>
    <#elseif section = "socialProviders" >
        <#if realm.password && social.providers??>
            <div id="kc-social-providers" class="mt-6">
                <div class="relative">
                    <div class="absolute inset-0 flex items-center">
                        <div class="w-full border-t border-gray-300"></div>
                    </div>
                    <div class="relative flex justify-center text-sm">
                        <span class="px-2 bg-white text-gray-500">Or continue with</span>
                    </div>
                </div>

                <div class="mt-6 grid grid-cols-1 gap-3">
                    <#list social.providers as p>
                        <a id="social-${p.alias}" class="w-full flex items-center justify-center px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
                            type="button" href="${p.loginUrl}">
                            <#if p.iconClasses?has_content>
                                <i class="${properties.kcCommonLogoIdP!} ${p.iconClasses!} mr-2" aria-hidden="true"></i>
                            <#else>
                                <span class="${properties.kcFormSocialAccountNameClass!}">${p.displayName!}</span>
                            </#if>
                        </a>
                    </#list>
                </div>
            </div>
        </#if>
    </#if>
</@layout.registrationLayout>
