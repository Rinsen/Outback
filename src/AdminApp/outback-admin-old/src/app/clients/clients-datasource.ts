import { DataSource } from '@angular/cdk/collections';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { Observable, of as observableOf, merge, BehaviorSubject } from 'rxjs';
import { ClientClient, OutbackClient } from '../services/generated/api.generated';

/**
 * Data source for the Clients view. This class should
 * encapsulate all logic for fetching and manipulating the displayed data
 * (including sorting, pagination, and filtering).
 */
export class ClientsDataSource extends DataSource<OutbackClient> {
  data: OutbackClient[] = new Array;
  paginator: MatPaginator | undefined;
  sort: MatSort | undefined;

  private clientsSubject = new BehaviorSubject<OutbackClient[]>([]);
  private loadingSubject = new BehaviorSubject<boolean>(false);

  public loading$ = this.loadingSubject.asObservable();

  constructor(private clientService: ClientClient) {
    super();
  }

  /**
   * Connect this data source to the table. The table will only update when
   * the returned stream emits new items.
   * @returns A stream of the items to be rendered.
   */
  connect(): Observable<OutbackClient[]> {
    return this.clientsSubject.asObservable();
  }

  /**
   *  Called when the table is being destroyed. Use this function, to clean up
   * any open connections or free any held resources that were set up during connect.
   */
  disconnect(): void {
    this.clientsSubject.complete();
    this.loadingSubject.complete();
  }

  loadClients() {
    this.loadingSubject.next(true);

    this.clientService.getAll()
      .subscribe(clients => {
        this.loadingSubject.next(false)
        this.clientsSubject.next(clients)
      });
  }

  /**
   * Paginate the data (client-side). If you're using server-side pagination,
   * this would be replaced by requesting the appropriate data from the server.
   */
  private getPagedData(data: OutbackClient[]): OutbackClient[] {
    if (this.paginator) {
      const startIndex = this.paginator.pageIndex * this.paginator.pageSize;
      return data.splice(startIndex, this.paginator.pageSize);
    } else {
      return data;
    }
  }

  /**
   * Sort the data (client-side). If you're using server-side sorting,
   * this would be replaced by requesting the appropriate data from the server.
   */
  private getSortedData(data: OutbackClient[]): OutbackClient[] {
    if (!this.sort || !this.sort.active || this.sort.direction === '') {
      return data;
    }

    return data.sort((a, b) => {
      const isAsc = this.sort?.direction === 'asc';
      switch (this.sort?.active) {
        case 'name': return compare(a.name? a.name : "" , b.name? b.name : "", isAsc);
        case 'description': return compare(a.description? a.description : "", b.description? b.description : "", isAsc);
        default: return 0;
      }
    });
  }
}

/** Simple sort comparator for example ID/Name columns (for client-side sorting). */
function compare(a: string | number, b: string | number, isAsc: boolean): number {
  return (a < b ? -1 : 1) * (isAsc ? 1 : -1);
}
