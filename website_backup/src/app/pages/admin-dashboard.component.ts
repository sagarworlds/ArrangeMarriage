import { Component } from '@angular/core';
import { ApiService } from '../services\api.service';

@Component({
  selector: 'app-admin-dashboard',
  template: `
    <div class="admin-container">
      <h1>Matchmaker Admin Panel</h1>
      <div class="kpis">
        <div class="kpi-box">Total Candidates: {{ totalCandidates }}</div>
        <div class="kpi-box">Pending Payments: {{ pendingPayments }}</div>
        <div class="kpi-box">Successful Marriages: {{ successCount }}</div>
      </div>
      <div class="management">
        <h2>Candidate Management</h2>
        <button (click)="loadCandidates()">Refresh List</button>
        <table>
          <thead>
            <tr><th>Name</th><th>Gender</th><th>Role</th><th>Status</th><th>Action</th></tr>
          </thead>
          <tbody>
            <tr *ngFor="let c of candidates">
              <td>{{ c.fullName }}</td>
              <td>{{ c.gender }}</td>
              <td>{{ c.role }}</td>
              <td>{{ c.isVerified ? 'Verified' : 'Pending' }}</td>
              <td><button (click)="verify(c.userId)">Verify</button></td>
            </tr>
          </tbody>
        </table>
      </div}
    </div>
  `,
  styles: [`
    .admin-container { padding: 2rem; font-family: sans-serif; }
    .kpis { display: flex; gap: 2rem; margin-bottom: 2rem; }
    .kpi-box { padding: 2rem; background: #f0f0f0; border-radius: 10px; font-weight: bold; }
    table { width: 100%; border-collapse: collapse; margin-top: 1rem; }
    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
  `]
})
export class AdminDashboardComponent {
  totalCandidates = 0;
  pendingPayments = 0;
  successCount = 0;
  candidates: any[] = [];

  constructor(private apiService: ApiService) {}

  loadCandidates() {
    // In real app, call API to get all profiles
  }

  verify(userId: string) {
    // call apiService.verifyUser(userId)
  }
}
