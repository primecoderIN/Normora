<#macro registrationLayout bodyClass="" displayInfo=false displayMessage=true displayRequiredFields=false showAnotherWayIfPresent=true>
<!DOCTYPE html>
<html class="h-full bg-slate-50">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>${msg("loginTitle",(realm.displayName!''))}</title>
    
    <#if properties.meta?has_content>
        <#list properties.meta?split(' ') as meta>
            <meta name="${meta?split('==')[0]}" content="${meta?split('==')[1]}"/>
        </#list>
    </#if>
    <#if properties.stylesCommon?has_content>
        <#list properties.stylesCommon?split(' ') as style>
            <link href="${url.resourcesCommonPath}/${style}" rel="stylesheet" />
        </#list>
    </#if>
    <#if properties.styles?has_content>
        <#list properties.styles?split(' ') as style>
            <link href="${url.resourcesPath}/${style}" rel="stylesheet" />
        </#list>
    </#if>
    <#if properties.scripts?has_content>
        <#list properties.scripts?split(' ') as script>
            <script src="${script}" type="text/javascript"></script>
        </#list>
    </#if>
</head>
<body class="h-full flex flex-col justify-center relative overflow-x-hidden bg-[#fafcff]">
    <!-- Background Decorators -->
    <div class="absolute top-0 right-0 w-[800px] h-[800px] bg-blue-50 rounded-full blur-[100px] opacity-60 -translate-y-1/2 translate-x-1/3 pointer-events-none"></div>
    <div class="absolute bottom-0 left-0 w-[600px] h-[600px] bg-indigo-50 rounded-full blur-[100px] opacity-80 translate-y-1/3 -translate-x-1/4 pointer-events-none"></div>

    <div class="relative z-10 flex flex-col items-center justify-center min-h-screen p-4">
        
        <!-- Card -->
        <div class="w-full max-w-[440px] bg-white rounded-2xl shadow-xl shadow-slate-200/50 p-10 border border-slate-100">
            <!-- Alert Messages -->
            <#if displayMessage && message?has_content && (message.type != 'warning' || !isAppInitiatedAction??)>
                <div class="mb-8 px-4 py-3 rounded-lg text-sm font-medium ${
                    (message.type = 'success')?string('bg-green-50 text-green-700 border border-green-100', 
                    (message.type = 'warning')?string('bg-yellow-50 text-yellow-700 border border-yellow-100', 
                    (message.type = 'error')?string('bg-red-50 text-red-700 border border-red-100', 'bg-blue-50 text-blue-700 border border-blue-100')))}">
                    ${kcSanitize(message.summary)?no_esc}
                </div>
            </#if>

            <#nested "header">
            <#nested "form">
            <#nested "socialProviders">
            <#nested "info">
        </div>

        <!-- Footer -->
        <footer class="mt-8 text-center text-xs text-slate-500 space-y-2">
            <div class="flex justify-center space-x-4">
                <a href="#" class="hover:text-slate-800 transition">Privacy Policy</a>
                <span>|</span>
                <a href="#" class="hover:text-slate-800 transition">Terms of Service</a>
                <span>|</span>
                <a href="#" class="hover:text-slate-800 transition">Help</a>
            </div>
            <p>© 2026 Normora. All rights reserved.</p>
        </footer>
    </div>
</body>
</html>
</#macro>
