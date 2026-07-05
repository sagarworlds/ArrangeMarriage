import { Component } from '@angular/core';

@Component({
  selector: 'app-home',
  template: `
    <div class="home-container">
      <header>
        <h1>Welcome to MatchMaker</h1>
        <p>Finding the perfect soulmate for a lifetime of happiness.</p>
      </header>
      <section class="cta">
        <button routerLink="/register">Register Now</button>
        <button routerLink="/services">Our Services</button>
      </section>
      <section class="features">
        <div class="feature-card">
          <h3>Verified Profiles</h3>
          <p>Every profile is verified by our expert team.</p>
        </div>
        <div class="feature-card">
          <h3>AI Matching</h3>
          <p>Smart algorithms to find the best compatibility.</p>
        </div>
        <div class="feature-card">
          <h3>Privacy Guaranteed</h3>
          <p>Your data is safe with us.</p>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .home-container { text-align: center; padding: 2rem; font-family: Arial, sans-serif; }
    header h1 { font-size: 3rem; color: #d32f2f; }
    .cta { margin: 2rem 0; }
    button { padding: 1rem 2rem; font-size: 1.2rem; margin: 0 1rem; cursor: pointer; background: #d32f2f; color: white; border: none; border-radius: 5px; }
    .features { display: flex; justify-content: space-around; margin-top: 3rem; }
    .feature-card { width: 30%; padding: 1rem; border: 1px solid #ddd; border-radius: 10px; }
  `]
})
export class HomeComponent {}
