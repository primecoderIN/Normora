<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('firstName','lastName','email','username','password','password-confirm'); section>
    <#if section = "header">
        ${msg("registerTitle")}
    <#elseif section = "form">
    <div id="kc-form">
      <div id="kc-form-wrapper" class="w-full max-w-md mx-auto">
        <form id="kc-register-form" class="${properties.kcFormClass!} space-y-6" action="${url.registrationAction}" method="post">
            
            <#if !realm.registrationEmailAsUsername>
                <div class="${properties.kcFormGroupClass!}">
                    <label for="username" class="block text-sm font-medium text-gray-700">${msg("username")}</label>
                    <div class="mt-1">
                        <input type="text" id="username" class="normora-input appearance-none block w-full px-3 py-2 border rounded-md shadow-sm placeholder-gray-400 focus:outline-none sm:text-sm" name="username"
                               value="${(register.formData.username!'')}" autocomplete="username"
                               aria-invalid="<#if messagesPerField.existsError('username')>true</#if>"
                        />
                    </div>
                </div>
            </#if>

            <div class="${properties.kcFormGroupClass!} flex gap-4">
                <div class="w-1/2">
                    <label for="firstName" class="block text-sm font-medium text-gray-700">${msg("firstName")}</label>
                    <div class="mt-1">
                        <input type="text" id="firstName" class="normora-input appearance-none block w-full px-3 py-2 border rounded-md shadow-sm placeholder-gray-400 focus:outline-none sm:text-sm" name="firstName"
                               value="${(register.formData.firstName!'')}"
                               aria-invalid="<#if messagesPerField.existsError('firstName')>true</#if>"
                        />
                    </div>
                </div>

                <div class="w-1/2">
                    <label for="lastName" class="block text-sm font-medium text-gray-700">${msg("lastName")}</label>
                    <div class="mt-1">
                        <input type="text" id="lastName" class="normora-input appearance-none block w-full px-3 py-2 border rounded-md shadow-sm placeholder-gray-400 focus:outline-none sm:text-sm" name="lastName"
                               value="${(register.formData.lastName!'')}"
                               aria-invalid="<#if messagesPerField.existsError('lastName')>true</#if>"
                        />
                    </div>
                </div>
            </div>

            <div class="${properties.kcFormGroupClass!}">
                <label for="email" class="block text-sm font-medium text-gray-700">${msg("email")}</label>
                <div class="mt-1">
                    <input type="email" id="email" class="normora-input appearance-none block w-full px-3 py-2 border rounded-md shadow-sm placeholder-gray-400 focus:outline-none sm:text-sm" name="email"
                           value="${(register.formData.email!'')}" autocomplete="email"
                           aria-invalid="<#if messagesPerField.existsError('email')>true</#if>"
                    />
                </div>
            </div>

            <#if passwordRequired??>
                <div class="${properties.kcFormGroupClass!}">
                    <label for="password" class="block text-sm font-medium text-gray-700">${msg("password")}</label>
                    <div class="mt-1">
                        <input type="password" id="password" class="normora-input appearance-none block w-full px-3 py-2 border rounded-md shadow-sm placeholder-gray-400 focus:outline-none sm:text-sm" name="password"
                               autocomplete="new-password"
                               aria-invalid="<#if messagesPerField.existsError('password','password-confirm')>true</#if>"
                        />
                    </div>
                </div>

                <div class="${properties.kcFormGroupClass!}">
                    <label for="password-confirm" class="block text-sm font-medium text-gray-700">${msg("passwordConfirm")}</label>
                    <div class="mt-1">
                        <input type="password" id="password-confirm" class="normora-input appearance-none block w-full px-3 py-2 border rounded-md shadow-sm placeholder-gray-400 focus:outline-none sm:text-sm" name="password-confirm"
                               aria-invalid="<#if messagesPerField.existsError('password-confirm')>true</#if>"
                        />
                    </div>
                </div>
            </#if>

            <#if recaptchaRequired??>
                <div class="form-group">
                    <div class="${properties.kcInputWrapperClass!}">
                        <div class="g-recaptcha" data-size="compact" data-sitekey="${recaptchaSiteKey}"></div>
                    </div>
                </div>
            </#if>

            <div class="${properties.kcFormGroupClass!}">
                <div id="kc-form-options" class="mb-4 text-sm">
                    <span><a class="font-medium text-indigo-600 hover:text-indigo-500" href="${url.loginUrl}">${kcSanitize(msg("backToLogin"))?no_esc}</a></span>
                </div>

                <div id="kc-form-buttons">
                    <button class="normora-btn-primary w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-bold focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500" type="submit">
                        ${msg("doRegister")}
                    </button>
                </div>
            </div>
        </form>
      </div>
    </div>
    </#if>
</@layout.registrationLayout>
