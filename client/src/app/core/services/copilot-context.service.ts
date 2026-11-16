import { Injectable, inject, signal, computed } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';

export type PersonaId = 'general' | 'hr' | 'finance' | 'legal' | 'security';

export interface AgentPersona {
  id: PersonaId;
  name: string;
  roleTitle: string;
  icon: string;
  colorClass: string;
  description: string;
  suggestedPrompts: string[];
}

export interface CopilotContext {
  pageTitle: string;
  pageCategory: 'documents' | 'departments' | 'user-groups' | 'dashboard' | 'employees' | 'settings' | 'general';
  suggestedPrompts: string[];
}

export const AGENT_PERSONAS: AgentPersona[] = [
  {
    id: 'general',
    name: 'General',
    roleTitle: 'Company Knowledge Assistant',
    icon: 'pi pi-sparkles',
    colorClass: 'text-primary-600',
    description: 'Broad enterprise intelligence synthesizing all indexed company documentation.',
    suggestedPrompts: [
      'What are our core operating values and working norms?',
      'Summarize our standard onboarding process',
      'Where do I find information about internal mobility?'
    ]
  },
  {
    id: 'hr',
    name: 'People & HR',
    roleTitle: 'HR & People Operations Agent',
    icon: 'pi pi-users',
    colorClass: 'text-purple-600',
    description: 'Expert in leave policies, benefits, performance reviews, and employee relations.',
    suggestedPrompts: [
      'What are the maternity and paternity leave entitlements?',
      'How does the annual performance cycle work?',
      'What healthcare and wellness benefits are provided?'
    ]
  },
  {
    id: 'finance',
    name: 'Finance & Travel',
    roleTitle: 'Finance & Procurement Agent',
    icon: 'pi pi-wallet',
    colorClass: 'text-emerald-600',
    description: 'Specialist in travel expense caps, per diems, corporate cards, and vendor invoicing.',
    suggestedPrompts: [
      'What is our daily meal and accommodation allowance for domestic travel?',
      'How do I submit an expense report for client entertainment?',
      'What is the approval matrix for software tool purchases?'
    ]
  },
  {
    id: 'legal',
    name: 'Legal & Compliance',
    roleTitle: 'Legal & Risk Operations Agent',
    icon: 'pi pi-shield',
    colorClass: 'text-amber-600',
    description: 'Specialist on NDAs, data privacy regulations (GDPR/DPDP), IP rights, and contracts.',
    suggestedPrompts: [
      'What is the standard procedure for getting an NDA signed with a vendor?',
      'What are our customer data retention guidelines under GDPR?',
      'Who is authorized to sign commercial agreements on behalf of the company?'
    ]
  },
  {
    id: 'security',
    name: 'InfoSec & IT',
    roleTitle: 'Information Security Agent',
    icon: 'pi pi-lock',
    colorClass: 'text-blue-600',
    description: 'Expert on password policies, device encryption, VPN, phishing alerts, and access keys.',
    suggestedPrompts: [
      'What are our company password strength and rotation policies?',
      'How do I request elevated GitHub and cloud repository access?',
      'How do I report a suspected phishing email or compromised credential?'
    ]
  }
];

@Injectable({
  providedIn: 'root'
})
export class CopilotContextService {
  private router = inject(Router);

  readonly currentRoute = signal<string>(this.router.url);
  readonly activePersonaId = signal<PersonaId>('general');
  readonly personas = AGENT_PERSONAS;

  readonly activePersona = computed<AgentPersona>(() => {
    return this.personas.find(p => p.id === this.activePersonaId()) || this.personas[0];
  });

  constructor() {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event: NavigationEnd) => {
        this.currentRoute.set(event.urlAfterRedirects || event.url);
      });
  }

  setPersona(id: PersonaId): void {
    this.activePersonaId.set(id);
  }

  readonly activeContext = computed<CopilotContext>(() => {
    if (this.activePersonaId() !== 'general') {
      const persona = this.activePersona();
      return {
        pageTitle: persona.roleTitle,
        pageCategory: 'general',
        suggestedPrompts: persona.suggestedPrompts
      };
    }

    const url = this.currentRoute().toLowerCase();

    if (url.includes('/employer/documents')) {
      return {
        pageTitle: 'Document Management',
        pageCategory: 'documents',
        suggestedPrompts: [
          'What remote work and expense policies are uploaded?',
          'Which documents have inactive or outdated versions?',
          'How do department clearances affect document visibility?'
        ]
      };
    }

    if (url.includes('/employer/departments') || url.includes('/employer/user-groups')) {
      return {
        pageTitle: 'Access Control & Departments',
        pageCategory: 'departments',
        suggestedPrompts: [
          'How do user groups map to department clearances?',
          'Which departments have access to restricted policies?',
          'Explain our role-based authorization model'
        ]
      };
    }

    if (url.includes('/employer/dashboard')) {
      return {
        pageTitle: 'Admin Overview',
        pageCategory: 'dashboard',
        suggestedPrompts: [
          'Summarize our workspace knowledge base status',
          'What policies are most frequently referenced?',
          'Which departments have documents uploaded?'
        ]
      };
    }

    if (url.includes('/employer/employees')) {
      return {
        pageTitle: 'Employee Directory',
        pageCategory: 'employees',
        suggestedPrompts: [
          'How do I invite a new employee with department access?',
          'What roles exist in this workspace?',
          'How does member deactivation and soft-delete work?'
        ]
      };
    }

    if (url.includes('/employer/settings')) {
      return {
        pageTitle: 'Workspace Settings',
        pageCategory: 'settings',
        suggestedPrompts: [
          'What are our workspace plan quotas and seat limits?',
          'How do dynamic white-label branding styles apply?',
          'What actions are recorded in the workspace audit log?'
        ]
      };
    }

    return {
      pageTitle: 'Knowledge Assistant',
      pageCategory: 'general',
      suggestedPrompts: [
        'What is our company travel and hotel reimbursement cap?',
        'What are the guidelines for remote work and equipment?',
        'How do I escalate an inquiry to HR administration?'
      ]
    };
  });
}
