import { Injectable } from '@angular/core';
import { ClientClient, CreateClient, OutbackClient } from './generated/api.generated';
import { Observable, of, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})

export class ClientServiceService {

  private clients : OutbackClient[] = new Array;

  constructor(private clientClient: ClientClient) { }

  public getClient(id: string) : Observable<OutbackClient> {
    if (this.clients.length > 0) {
      return new Observable<OutbackClient>(subscriber => {
        subscriber.next(this.clients.find(c => c.clientId === id));
      });
    }

    return this.clientClient.get(id);
  }

  public saveClient(client: OutbackClient) : Observable<boolean> {

    if (client.clientId !== undefined) {
      this.clientClient.update(client.clientId, client);
      return of(true);
    }

    return of(false);
  }

  public getClients() : Observable<OutbackClient[]> {
    if (this.clients.length > 0) {
      return new Observable<OutbackClient[]>(subscriber => {
        subscriber.next(this.clients);
      });
    }

    return this.clientClient.getAll().pipe(tap(c => this.clients.push(...c)));
  }

  public refetchClients() : Observable<OutbackClient[]> {
    this.clients.length = 0;
    return this.clientClient.getAll().pipe(tap(c => this.clients.push(...c)));
  }

  public deleteClient(id: string) : Observable<boolean> {
    this.clientClient.delete(id);
    return of(true);
  }

  public createClient(client: CreateClient) : Observable<boolean> {
    this.clientClient.create(client);
    return of(true);
  }
}
