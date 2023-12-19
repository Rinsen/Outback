import { AfterViewInit, Component, ViewChild } from '@angular/core';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatTable } from '@angular/material/table';
import { ClientsDataSource } from './clients-datasource';
import { Router }          from '@angular/router';
import { MatSelectModule } from '@angular/material/select';
import { ClientClient, ClientType, CreateClient, OutbackClient, OutbackClientFamily } from '../services/generated/api.generated';

@Component({
  selector: 'app-clients',
  templateUrl: './clients.component.html',
  styleUrls: ['./clients.component.css']
})
export class ClientsComponent implements AfterViewInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatTable) table!: MatTable<OutbackClient>;
  dataSource: ClientsDataSource;

  /** Columns displayed in the table. Columns IDs can be added, removed, or reordered. */
  displayedColumns = ['id', 'name', 'description'];
  newClient: CreateClient = new CreateClient({ clientName: '', description: '', clientType: ClientType._0, familyId: 0 });
  families: OutbackClientFamily[] = new Array;
  selectedClientType: string = 'confidential';
  createClientOpen = false;  

  constructor(protected clientClient: ClientClient, protected router: Router) {
    this.dataSource = new ClientsDataSource(clientClient);
  }

  rowClicked(row: OutbackClient)
  {
    this.router.navigate(['/clientdetail/' + row.clientId]);
  }

  beginCreateClient() {
    this.clientClient.getFamily()
      .subscribe(famResponse => {
        this.families.length = 0;

        var first = true;
        famResponse.forEach(fam => {
          if (first){
            this.newClient.familyId = fam.id as number;
            first = false;
          }
          
          this.families.push(fam);
        });

        this.createClientOpen = true;
      });
  }

  saveClient() {
    switch (this.selectedClientType){
      case 'confidential':
        this.newClient.clientType = ClientType._0;
        break;
      case 'credentialed':
        this.newClient.clientType = ClientType._1
        break;
      default:
        return;
    }

    this.clientClient.create(this.newClient)
      .subscribe(createdClient => {
        
        this.dataSource.loadClients();
        this.createClientOpen = false;
      });
  }

  cancelCreateClient() {
    this.createClientOpen = false;
  }

  ngOnInit(): void {
    this.dataSource.loadClients();
  }

  ngAfterViewInit(): void {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
    this.table.dataSource = this.dataSource;
  }
}
