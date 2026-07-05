import { Component } from '@angular/core';
import { ApiService } from '../services\api.service';
import { Profile } from '../models\api-models';

@Component({
  selector: 'app-member-dashboard',
  template: `
    <div class="dashboard">
      <h2>Welcome, {{ userProfile?.fullName }}</h2>
      <div class="stats">
        <div class="card">Matches: {{ matches?.length || 0 }}</div>
        <div class="card">Proposals Sent: {{ proposalsSent }}</div>
        <div class="card">Proposals Received: {{ proposalsReceived }}</div>
      </div>
      <div class="actions">
        <button (click)="searchMatches()">Find Matches</button>
        <button routerLink="/profile/edit">Edit Profile</button>
      </div>
      <div class="match-list" *ngIf="matches">
        <h3>Suggested Matches</h3>
        <div *ngFor="let match of matches" class="match-item">
          <p>{{ match.fullName }} - {{ match.religion }}, {{ match.education }}</p>
          <button (click)="sendProposal(match.userId)">Send Proposal</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard { padding: 2rem; font-family: sans-serif; }
    .stats { display: flex; gap: 1rem; margin: 1rem 0; }
    .card { padding: 1rem; border: 1px solid #ccc; border-radius: 8px; flex: 1; text-align: center; }
    .actions { margin-bottom: 2rem; }
    .match-item { padding: 1rem; border-bottom: 1px solid #eee; display: flex; justify-content: space-between; }
  `]
})
export class MemberDashboardComponent {
  userProfile?: Profile;
  matches: Profile[] = [];
  proposalsSent = 0;
  proposalsReceived = 0;

  constructor(private apiService: ApiService) {}

  searchMatches() {
    const userId = 'current-user-id'; // Mocked
    this.apiService.searchMatches(userId).subscribe(res => this.matches = res);
  }

  sendProposal(receiverId: string) {
    const userId = 'current-user-id'; // Mocked
    this.apiService.sendProposal({ senderId: userId, receiverId, notes: 'I am interested' }).subscribe(() => {
      alert('Proposal sent!');
    });
  }
}
