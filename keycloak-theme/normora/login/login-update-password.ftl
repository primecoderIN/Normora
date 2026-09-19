<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('password','password-confirm'); section>
    <#if section = "header">
        ${msg("updatePasswordTitle")}
    <#elseif section = "form">
    <div id="kc-form">
      <div id="kc-form-wrapper" class="w-full max-w-md mx-auto">
        <form id="kc-passwd-update-form" class="${properties.kcFormClass!} space-y-6" action="${url.loginAction}" method="post">
            <input type="text" id="username" name="username" value="${username}" autocomplete="username"
                   readonly="readonly" style="display:none;"/>
            <input type="password" id="password" name="password" autocomplete="current-password" style="display:none;"/>

            <div class="${properties.kcFormGroupClass!}">
                <label for="password-new" class="block text-sm font-medium text-gray-700">${msg("passwordNew")}</label>
                <div class="mt-1">
                    <input type="password" id="password-new" name="password-new" class="normora-input appearance-none block w-full px-3 py-2 border rounded-md shadow-sm placeholder-gray-400 focus:outline-none sm:text-sm" autofocus autocomplete="new-password"
                           aria-invalid="<#if messagesPerField.existsError('password','password-confirm')>true</#if>"
                    />
                </div>
            </div>

            <div class="${properties.kcFormGroupClass!}">
                <label for="password-confirm" class="block text-sm font-medium text-gray-700">${msg("passwordConfirm")}</label>
                <div class="mt-1">
                    <input type="password" id="password-confirm" name="password-confirm"
                           class="normora-input appearance-none block w-full px-3 py-2 border rounded-md shadow-sm placeholder-gray-400 focus:outline-none sm:text-sm"
                           autocomplete="new-password"
                           aria-invalid="<#if messagesPerField.existsError('password-confirm')>true</#if>"
                    />
                </div>
            </div>

            <div class="${properties.kcFormGroupClass!} flex items-center justify-between mt-4">
                <div id="kc-form-options">
                    <#if isAppInitiatedAction??>
                        <button type="submit" class="bg-gray-200 hover:bg-gray-300 text-gray-800 font-bold py-2 px-4 rounded" name="cancel-aia" value="true" />${msg("doCancel")}</button>
                    </#if>
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
    </#if>
</@layout.registrationLayout>
