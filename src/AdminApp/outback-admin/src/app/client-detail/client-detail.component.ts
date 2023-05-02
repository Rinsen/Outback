import { Component, OnInit } from '@angular/core';
import { throwToolbarMixedModesError } from '@angular/material/toolbar';
import { ActivatedRoute } from '@angular/router';
import {MatTableModule, MatTableDataSource} from '@angular/material/table';
import { ClientClient, ClientType, OutbackClient, OutbackClientFamily, OutbackClientLoginRedirectUri, OutbackClientScope, OutbackScope, ScopeClient } from '../modules/api/api.generated';

@Component({
  selector: 'app-client-detail',
  templateUrl: './client-detail.component.html',
  styleUrls: ['./client-detail.component.css']
})

export class ClientDetailComponent implements OnInit {
  id: string = '';
  client = new OutbackClient();
  scopes: OutbackScope[] = new Array;
  dataSource = new MatTableDataSource;
  displayedColumns: string[] = ['scopeName', 'scopeDescription'];
  editScopesOpen: boolean = false;

  constructor(private route: ActivatedRoute,
    private clientClient: ClientClient,
    private scopeClient: ScopeClient) {

    this.id = String(this.route.snapshot.paramMap.get('id'));
    this.clientClient.get(this.id)
    .subscribe(clientResponse => {
        this.client = clientResponse;
        this.dataSource.data = this.client.scopes as unknown[];
      });
   }

   openEditScope() {
     this.scopeClient.getAll()
      .subscribe(scopeResponse => {
        scopeResponse.forEach(scope => {
          this.scopes.push(scope);
        });
        this.editScopesOpen = true;
      });
   }

   closeEditScope(){
    this.scopes.length = 0;
    this.editScopesOpen = false;
   }

   addScopeToClient(id: number) {
    const selectedScope = this.getScope(id);

    if (selectedScope === null){
      return;
    }
    
    if (!this.client.scopes?.some(( {scope}) => scope?.id === id)) {
      this.client.scopes?.push(new OutbackClientScope({
        clientId: this.client.id,
        scope: selectedScope,
        scopeId: selectedScope?.id
      }));
      this.dataSource.data = this.client.scopes as unknown[];
    }
   }

   removeScopeToClient(id: number) {
    const selectedScope = this.getScope(id);

    if (selectedScope === null){
      return;
    }
    
    var scopeToRemove: OutbackScope | null = null;
    if (this.client.scopes?.some(( {scope}) => scope?.id === id)) {
      this.client.scopes.forEach(scope => {
        if (scope.id === selectedScope.id) {
          scopeToRemove = scope;
        }
      });
    }

    if (scopeToRemove !== null) {
      const index = this.client.scopes?.indexOf(scopeToRemove);

      if (index !== undefined && index !== -1) {
        this.client.scopes?.splice(index, 1);
      }
    }
   }

   getScope(id: number) : OutbackScope | null {
    var returnScope: OutbackScope | null = null;

    this.scopes.forEach(scope => {
      if (scope.id === id){
        returnScope = scope;
      }
    });

    return returnScope;
   }

   onAddLoginRedirectUri(){
     this.client.loginRedirectUris?.push(new OutbackClientLoginRedirectUri())
   }

   onSaveClient(){
    this.clientClient.update(this.client.clientId, this.client)
    .subscribe(clientResponse => {
      
    });
   }

  ngOnInit(): void {

  }
}
