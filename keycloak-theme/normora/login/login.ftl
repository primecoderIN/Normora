<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('username','password') displayInfo=realm.password && realm.registrationAllowed && !registrationDisabled??; section>
    <#if section = "header">
        <div class="flex flex-col items-center mb-8">
            <div class="flex items-center space-x-2 mb-1">
                <div class="w-8 h-8 bg-blue-600 rounded flex items-center justify-center text-white font-bold text-lg">N</div>
                <div class="flex flex-col leading-none">
                    <span class="font-bold text-xl text-slate-900 tracking-tight">Normora</span>
                    <span class="text-[10px] text-slate-500 font-medium tracking-wide">Knowledge for a smarter tomorrow</span>
                </div>
            </div>
        </div>
        <h1 class="text-3xl font-bold text-slate-900 text-center mb-2">Welcome to Normora</h1>
        <p class="text-slate-500 text-center text-sm mb-8">Sign in to continue to your workspace.</p>
    <#elseif section = "form">
        <div id="kc-form" class="w-full">
            
            <#-- Social Providers -->
            <#if realm.password && social.providers??>
                <div class="space-y-3 mb-6">
                    <#list social.providers as p>
                        <a id="social-${p.alias}" class="w-full flex items-center justify-center gap-3 px-4 py-2.5 border border-slate-200 shadow-sm text-sm font-semibold rounded-lg text-slate-700 bg-white hover:bg-slate-50 transition" href="${p.loginUrl}">
                            <#if p.alias == 'google'>
                                <svg class="w-5 h-5" viewBox="0 0 24 24"><path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/><path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/><path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/><path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/></svg>
                            <#elseif p.alias == 'microsoft' || p.alias == 'github'>
                                <svg class="w-5 h-5" viewBox="0 0 21 21"><path fill="#f25022" d="M0 0h10v10H0z"/><path fill="#7fba00" d="M11 0h10v10H11z"/><path fill="#00a4ef" d="M0 11h10v10H0z"/><path fill="#ffb900" d="M11 11h10v10H11z"/></svg>
                            <#else>
                                <#if p.iconClasses?has_content>
                                    <i class="${properties.kcCommonLogoIdP!} ${p.iconClasses!}" aria-hidden="true"></i>
                                </#if>
                            </#if>
                            Continue with ${p.displayName!}
                        </a>
                    </#list>
                </div>

                <div class="relative mb-6">
                    <div class="absolute inset-0 flex items-center"><div class="w-full border-t border-slate-200"></div></div>
                    <div class="relative flex justify-center text-xs font-semibold tracking-widest text-slate-400 uppercase">
                        <span class="bg-white px-3">OR</span>
                    </div>
                </div>
            </#if>

            <#if realm.password>
                <form id="kc-form-login" onsubmit="login.disabled = true; return true;" action="${url.loginAction}" method="post" class="space-y-5">
                    
                    <div>
                        <label for="username" class="block text-sm font-semibold text-slate-700 mb-1.5">
                            <#if !realm.loginWithEmailAllowed>${msg("username")}<#elseif !realm.registrationEmailAsUsername>Username or email<#else>${msg("email")}</#if>
                        </label>
                        <div class="relative">
                            <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                <svg class="h-5 w-5 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></svg>
                            </div>
                            <input tabindex="1" id="username" class="block w-full pl-10 pr-3 py-2.5 border border-slate-200 rounded-lg text-sm placeholder-slate-400 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 transition" name="username" value="${(login.username!'')}" type="text" autofocus autocomplete="off" placeholder="you@company.com" aria-invalid="<#if messagesPerField.existsError('username','password')>true</#if>" />
                        </div>
                    </div>

                    <div>
                        <label for="password" class="block text-sm font-semibold text-slate-700 mb-1.5">Password</label>
                        <div class="relative">
                            <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                <svg class="h-5 w-5 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" /></svg>
                            </div>
                            <input tabindex="2" id="password" class="block w-full pl-10 pr-10 py-2.5 border border-slate-200 rounded-lg text-sm placeholder-slate-400 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 transition" name="password" type="password" autocomplete="off" placeholder="Enter your password" aria-invalid="<#if messagesPerField.existsError('username','password')>true</#if>" />
                            <div class="absolute inset-y-0 right-0 pr-3 flex items-center">
                                <button type="button" class="text-slate-400 hover:text-slate-600 focus:outline-none" onclick="const p = document.getElementById('password'); p.type = p.type === 'password' ? 'text' : 'password';">
                                    <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" /></svg>
                                </button>
                            </div>
                        </div>
                        <#if realm.resetPasswordAllowed>
                            <div class="flex justify-end mt-2">
                                <a tabindex="5" href="${url.loginResetCredentialsUrl}" class="text-sm font-semibold text-blue-600 hover:text-blue-500 transition">Forgot password?</a>
                            </div>
                        </#if>
                    </div>

                    <div class="pt-2">
                        <button tabindex="4" class="w-full flex justify-center items-center gap-2 py-3 px-4 border border-transparent rounded-lg shadow-sm text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition" name="login" id="kc-login" type="submit">
                            Sign in 
                            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 5l7 7m0 0l-7 7m7-7H3" /></svg>
                        </button>
                    </div>
                </form>
            </#if>
        </div>
    <#elseif section = "info" >
        <#if realm.password && realm.registrationAllowed && !registrationDisabled??>
            <div id="kc-registration" class="mt-8 text-center text-sm text-slate-500 font-medium">
                New to Normora? <a tabindex="6" href="${url.registrationUrl}" class="font-semibold text-blue-600 hover:text-blue-500 transition">Create an account</a>
            </div>
        </#if>
    </#if>
</@layout.registrationLayout>
