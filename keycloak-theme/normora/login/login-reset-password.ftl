<#import "template.ftl" as layout>
<@layout.registrationLayout displayInfo=true displayMessage=!messagesPerField.existsError('username'); section>
    <#if section = "header">
        ${msg("emailForgotTitle")}
    <#elseif section = "form">
    <div id="kc-form">
      <div id="kc-form-wrapper" class="w-full max-w-md mx-auto">
        <form id="kc-reset-password-form" class="space-y-6" action="${url.loginAction}" method="post">
            <div class="${properties.kcFormGroupClass!}">
                <label for="username" class="block text-sm font-medium text-gray-700">
                    <#if !realm.loginWithEmailAllowed>${msg("username")}<#elseif !realm.registrationEmailAsUsername>${msg("usernameOrEmail")}<#else>${msg("email")}</#if>
                </label>
                <div class="mt-1">
                    <input type="text" id="username" name="username" class="normora-input appearance-none block w-full px-3 py-2 border rounded-md shadow-sm placeholder-gray-400 focus:outline-none sm:text-sm" autofocus value="${(auth.attemptedUsername!'')}" aria-invalid="<#if messagesPerField.existsError('username')>true</#if>"/>
                </div>
            </div>
            
            <div class="flex items-center justify-between">
                <div id="kc-form-options">
                    <span class="text-sm"><a class="font-medium text-indigo-600 hover:text-indigo-500" href="${url.loginUrl}">${kcSanitize(msg("backToLogin"))?no_esc}</a></span>
                </div>

                <div id="kc-form-buttons">
                    <button class="normora-btn-primary flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-bold focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500" type="submit">
                        ${msg("doSubmit")}
                    </button>
                </div>
            </div>
        </form>
      </div>
    </div>
    <#elseif section = "info" >
        <div class="mt-4 text-center text-sm text-gray-500">
            ${msg("emailInstruction")}
        </div>
    </#if>
</@layout.registrationLayout>
