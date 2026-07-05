import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { User, Profile } from '../models/api-models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = 'http://localhost:5000/api';

  constructor(private http: HttpClient) {}

  // User endpoints
  registerUser(user: any): Observable<User> {
    return this.http.post<User>(`${this.baseUrl}/user/register`, user);
  }

  getUser(id: string): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/user/${id}`);
  }

  // Profile endpoints
  createProfile(profile: any): Observable<Profile> {
    return this.http.post<Profile>(`${this.baseUrl}/profile`, profile);
  }

  getProfile(userId: string): Observable<Profile> {
    return this.http.get<Profile>(`${this.baseUrl}/profile/user/${userId}`);
  }

  updateProfile(profile: Profile): Observable<any> {
    return this.http.put(`${this.baseUrl}/profile`, profile);
  }

  // Matchmaking endpoints
  searchMatches(userId: string): Observable<Profile[]> {
    return this.http.get<Profile[]>(`${this.baseUrl}/matchmaking/search/${userId}`);
  }

  sendProposal(proposal: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/matchmaking/propose`, proposal);
  }
}
