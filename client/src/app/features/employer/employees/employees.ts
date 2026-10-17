import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe, CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { InvitationService } from '@core/services/invitation.service';
import { TenantService, TenantEmployeeDto } from '@core/services/tenant.service';
import { DepartmentService, Department } from '@core/services/department.service';
import { ButtonModule } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { SelectModule } from 'primeng/select';
import { MenuItem, MessageService } from 'primeng/api';
import { ApiMessages } from '@core/constants/api-messages';

@Component({
  selector: 'app-employees',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule, 
    FormsModule,
    ButtonModule, 
    DatePipe,
    MenuModule,
    PaginatorModule,
    SelectModule
  ],
  templateUrl: './employees.html',
})
export class Employees implements OnInit {
  private fb = inject(FormBuilder);
  private invitationService = inject(InvitationService);
  private tenantService = inject(TenantService);
  private departmentService = inject(DepartmentService);
  private messageService = inject(MessageService);

  inviteForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
  });

  isInviting = signal(false);
  isLoadingEmployees = signal(true);
  
  // Data
  employees = signal<TenantEmployeeDto[]>([]);
  departments = signal<Department[]>([]);
  
  // Stats
  pendingInvites = signal<number>(0);
  adminSeats = signal<number>(0);
  totalEmployees = signal<number>(0);
  
  // UI Messages
  successMessage = signal('');
  errorMessage = signal('');

  // Pagination & Filtering
  currentPage = signal(1);
  pageSize = signal(10);
  totalRecords = signal(0);
  totalPages = signal(0);
  searchQuery = signal('');
  selectedDepartmentId = signal<string | null>(null);

  // Actions
  menuItems: MenuItem[] = [];
  targetEmployee = signal<TenantEmployeeDto | null>(null);
  
  employeeToRemoveId = signal<string | null>(null);
  isRemoving = signal(false);

  private searchTimeout: ReturnType<typeof setTimeout> | null = null;

  readonly skeletonRows = [1, 2, 3, 4, 5];

  ngOnInit() {
    this.loadDepartments();
    this.loadEmployees();
    this.loadStats();
    this.initMenu();
  }

  private initMenu() {
    this.menuItems = [
      {
        label: 'Manage Assignments',
        icon: 'pi pi-sitemap',
        command: () => {
          this.messageService.add({ severity: 'info', summary: 'Coming Soon', detail: 'Manage assignments will be available in a future update.' });
        }
      },
      {
        separator: true
      },
      {
        label: 'Remove from workspace',
        icon: 'pi pi-user-minus',
        styleClass: 'text-red-600',
        command: () => {
          const target = this.targetEmployee();
          if (target) {
            this.confirmRemove(target.membershipId);
          }
        }
      }
    ];
  }

  setMenuTarget(emp: TenantEmployeeDto) {
    this.targetEmployee.set(emp);
  }

  loadDepartments() {
    this.departmentService.getAll().subscribe({
      next: (depts) => {
        this.departments.set(depts || []);
      },
      error: (err: unknown) => console.error('Failed to load departments', err)
    });
  }

  loadEmployees() {
    this.isLoadingEmployees.set(true);
    
    this.tenantService.getEmployees(
      this.currentPage(),
      this.pageSize(),
      this.searchQuery(),
      this.selectedDepartmentId() ?? undefined
    ).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.employees.set(res.data.items || []);
          this.totalRecords.set(res.data.totalCount || 0);
          this.totalPages.set(res.data.totalPages || 0);
          
          // Update total active employees stat only if no filters are applied
          if (!this.searchQuery() && !this.selectedDepartmentId()) {
            this.totalEmployees.set(res.data.totalCount || 0);
          }
        }
        this.isLoadingEmployees.set(false);
      },
      error: (err: unknown) => {
        console.error('Failed to load employees', err);
        this.isLoadingEmployees.set(false);
      }
    });
  }

  loadStats() {
    this.tenantService.getEmployeeStats().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.pendingInvites.set(res.data.pendingInvites);
          this.adminSeats.set(res.data.adminSeats);
        }
      },
      error: (err: unknown) => console.error('Failed to load stats', err)
    });
  }

  onPageChange(event: PaginatorState) {
    const first = event.first ?? 0;
    const rows = event.rows ?? 10;
    this.currentPage.set(Math.floor(first / rows) + 1);
    this.pageSize.set(rows);
    this.loadEmployees();
  }

  onSearchChange(value: string) {
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }
    this.searchTimeout = setTimeout(() => {
      this.searchQuery.set(value);
      this.currentPage.set(1);
      this.loadEmployees();
    }, 350);
  }

  onDepartmentChange(value: string | null) {
    this.selectedDepartmentId.set(value);
    this.currentPage.set(1);
    this.loadEmployees();
  }

  clearFilters() {
    this.searchQuery.set('');
    this.selectedDepartmentId.set(null);
    this.currentPage.set(1);
    this.loadEmployees();
  }

  confirmRemove(membershipId: string) {
    this.employeeToRemoveId.set(membershipId);
  }

  cancelRemove() {
    this.employeeToRemoveId.set(null);
  }

  executeRemove() {
    const membershipId = this.employeeToRemoveId();
    if (!membershipId) return;

    this.isRemoving.set(true);
    
    this.tenantService.removeEmployee(membershipId).subscribe({
      next: () => {
        this.isRemoving.set(false);
        this.employeeToRemoveId.set(null);
        this.loadEmployees();
        this.loadStats();
      },
      error: (err: { error?: { message?: string } }) => {
        console.error('Failed to remove employee', err);
        this.isRemoving.set(false);
        this.messageService.add({ severity: 'error', summary: ApiMessages.EmployeeRemoveFailed.summary, detail: err.error?.message || ApiMessages.EmployeeRemoveFailed.detail });
      }
    });
  }

  onSubmit() {
    if (this.inviteForm.invalid) return;

    this.isInviting.set(true);
    this.successMessage.set('');
    this.errorMessage.set('');

    const payload = this.inviteForm.value;

    this.invitationService.inviteEmployee(payload.email).subscribe({
      next: (res) => {
        this.isInviting.set(false);
        if (res.success) {
          this.successMessage.set(res.message);
          this.inviteForm.reset();
          this.loadStats();
        } else {
          this.errorMessage.set(res.message);
        }
      },
      error: (err: { error?: { message?: string } }) => {
        this.isInviting.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to send invitation');
      },
    });
  }

  exportEmployees() {
    this.messageService.add({ severity: 'info', summary: 'Export Started', detail: 'Your export is being generated and will download shortly.' });
  }
}
