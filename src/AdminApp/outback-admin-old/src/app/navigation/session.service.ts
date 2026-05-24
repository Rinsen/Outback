import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SessionInformation } from './sessionInformation';

@Injectable({
  providedIn: 'root'
})
export class SessionService {


  constructor(private httpClient: HttpClient) {

   }

   public getSessionInformation(observe?: 'body', reportProgress?: boolean): Observable<SessionInformation> {
    return this.httpClient.get<SessionInformation>('/api/Session');
   }
}