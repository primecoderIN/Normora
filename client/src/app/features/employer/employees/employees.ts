import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe, CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { InvitationService } from '@core/services/invitation.service';
import { TenantService } from '@core/services/tenant.service';
import { DepartmentService, Department } from '@core/services/department.service';
import { ButtonModule } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { PaginatorModule } from 'primeng/paginator';
import { SelectModule } from 'primeng/select';
import { MenuItem } from 'primeng/api';

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

  inviteForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
  });

  isInviting = signal(false);
  isLoadingEmployees = signal(true);
  
  // Data
  employees = signal<any[]>([]);
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
  targetEmployee = signal<any>(null);
  
  employeeToRemoveId = signal<string | null>(null);
  isRemoving = signal(false);

  readonly skeletonRows = [1, 2, 3, 4, 5];

  ngOnInit() {
    this.loadDepartments();
    this.loadEmployees();
    this.initMenu();
  }

  private initMenu() {
    this.menuItems = [
      {
        label: 'Manage Assignments',
        icon: 'pi pi-sitemap',
        command: () => {
          // Future phase feature
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

  setMenuTarget(emp: any) {
    this.targetEmployee.set(emp);
  }

  loadDepartments() {
    this.departmentService.getAll().subscribe({
      next: (depts) => {
        this.departments.set(depts || []);
      },
      error: (err) => console.error('Failed to load departments', err)
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
      error: (err) => {
        console.error('Failed to load employees', err);
        this.isLoadingEmployees.set(false);
      }
    });

    this.tenantService.getEmployeeStats().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.pendingInvites.set(res.data.pendingInvites);
          this.adminSeats.set(res.data.adminSeats);
        }
      },
      error: (err) => console.error('Failed to load stats', err)
    });
  }

  onPageChange(event: any) {
    this.currentPage.set(Math.floor(event.first / event.rows) + 1);
    this.pageSize.set(event.rows);
    this.loadEmployees();
  }

  onSearchChange(value: string) {
    this.searchQuery.set(value);
    this.currentPage.set(1);
    this.loadEmployees();
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
      },
      error: (err) => {
        console.error('Failed to remove employee', err);
        this.isRemoving.set(false);
        // You could add a toast message here if you injected MessageService
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
      next: (res: any) => {
        this.isInviting.set(false);
        if (res.success) {
          this.successMessage.set(res.message);
          this.inviteForm.reset();
          // Reload stats to update pending invites count
          this.tenantService.getEmployeeStats().subscribe(statsRes => {
             if (statsRes.success && statsRes.data) {
                this.pendingInvites.set(statsRes.data.pendingInvites);
             }
          });
        } else {
          this.errorMessage.set(res.message);
        }
      },
      error: (err: any) => {
        this.isInviting.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to send invitation');
      },
    });
  }
}
