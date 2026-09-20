<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=false; section>
    <#if section = "header">
        <div class="text-center mb-6">
            <div class="inline-flex items-center justify-center w-16 h-16 rounded-full bg-red-50 mb-4">
                <svg class="w-8 h-8 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path>
                </svg>
            </div>
            <h1 class="text-2xl font-bold text-slate-800 tracking-tight">Oops! Something went wrong</h1>
            <p class="text-slate-500 mt-2 text-sm font-medium">
                ${kcSanitize(message.summary)?no_esc}
            </p>
        </div>
    <#elseif section = "form">
        <div class="text-center text-slate-600 text-sm mb-2">
            <p class="mb-8">
                If you just used the browser's <strong>Back</strong> button, this is an expected security feature to protect your session. Please restart your login process.
            </p>
            
            <#if skipLink??>
            <#else>
                <#if client?? && client.baseUrl?has_content>
                    <a id="backToApplication" href="${client.baseUrl}" class="inline-flex justify-center w-full rounded-xl bg-blue-600 px-4 py-3 text-sm font-semibold text-white shadow-sm hover:bg-blue-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600 transition">
                        Restart Login
                    </a>
                </#if>
            </#if>
        </div>
    </#if>
</@layout.registrationLayout>
