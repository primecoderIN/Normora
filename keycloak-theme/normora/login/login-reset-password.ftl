<#import "template.ftl" as layout>
<@layout.registrationLayout displayInfo=true displayMessage=!messagesPerField.existsError('username'); section>
    <#if section = "header">
        <div class="flex flex-col items-center mb-8">
            <div class="flex items-center space-x-2 mb-8">
                <div class="w-8 h-8 bg-blue-600 rounded flex items-center justify-center text-white font-bold text-lg">N</div>
                <div class="flex flex-col leading-none">
                    <span class="font-bold text-xl text-slate-900 tracking-tight">Normora</span>
                    <span class="text-[10px] text-slate-500 font-medium tracking-wide">Your Knowledge Workspace</span>
                </div>
            </div>

            <!-- Envelope Icon with rays -->
            <div class="relative w-20 h-20 bg-blue-100 rounded-full flex items-center justify-center mb-6">
                <!-- Sparkles/Rays -->
                <div class="absolute -top-2 right-2 w-1.5 h-1.5 bg-blue-400 rounded-full"></div>
                <div class="absolute top-0 right-[-0.5rem] w-3 h-1 bg-blue-400 rounded-full rotate-45"></div>
                <div class="absolute top-4 right-[-1rem] w-1 h-3 bg-blue-400 rounded-full rotate-[15deg]"></div>
                <!-- Envelope -->
                <svg class="w-10 h-10 text-blue-600 relative z-10" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" /></svg>
                <!-- Blue arrow circle overlay -->
                <div class="absolute -bottom-2 -right-2 w-8 h-8 bg-blue-600 rounded-full flex items-center justify-center border-4 border-white shadow-sm z-20">
                    <svg class="w-4 h-4 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M14 5l7 7m0 0l-7 7m7-7H3" /></svg>
                </div>
            </div>

            <h1 class="text-2xl font-bold text-slate-900 text-center mb-3">Forgot your password?</h1>
            <p class="text-slate-500 text-center text-sm leading-relaxed max-w-xs mx-auto mb-2">
                Enter your username or email address and we will send you instructions on how to create a new password.
            </p>
        </div>
    <#elseif section = "form">
    <div id="kc-form" class="w-full">
        <form id="kc-reset-password-form" class="space-y-6" action="${url.loginAction}" method="post">
            
            <div>
                <label for="username" class="block text-sm font-semibold text-slate-700 mb-1.5">
                    Username or email
                </label>
                <div class="relative">
                    <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                        <svg class="h-5 w-5 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" /></svg>
                    </div>
                    <input type="text" id="username" name="username" class="block w-full pl-10 pr-3 py-2.5 border border-slate-200 rounded-lg text-sm placeholder-slate-400 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 transition" placeholder="you@company.com" autofocus value="${(auth.attemptedUsername!'')}" aria-invalid="<#if messagesPerField.existsError('username')>true</#if>"/>
                </div>
            </div>
            
            <button class="w-full flex justify-center items-center gap-2 py-3 px-4 border border-transparent rounded-lg shadow-sm text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition" type="submit">
                Send reset instructions
                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 5l7 7m0 0l-7 7m7-7H3" /></svg>
            </button>

            <div class="relative mt-8">
                <div class="absolute inset-0 flex items-center"><div class="w-full border-t border-slate-200"></div></div>
                <div class="relative flex justify-center">
                    <a href="${url.loginUrl}" class="bg-white px-4 text-sm font-semibold text-blue-600 hover:text-blue-500 transition">&laquo; Back to sign in</a>
                </div>
            </div>
        </form>
    </div>
    <#elseif section = "info" >
        <#-- Info moved to header section above -->
    </#if>
</@layout.registrationLayout>
