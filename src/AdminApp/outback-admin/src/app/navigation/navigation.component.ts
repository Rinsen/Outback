import { Component } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Observable } from 'rxjs';
import { map, shareReplay } from 'rxjs/operators';
import { SessionService } from './session.service';
import { SessionInformation } from './sessionInformation';
import { Inject, Injectable } from '@angular/core';  
import { DOCUMENT } from '@angular/common';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-navigation',
  templateUrl: './navigation.component.html',
  styleUrls: ['./navigation.component.css']
})
export class NavigationComponent {

  sessionInformation?: SessionInformation;

  isHandset$: Observable<boolean> = this.breakpointObserver.observe(Breakpoints.Handset)
    .pipe(
      map(result => result.matches),
      shareReplay()
    );

  constructor(private breakpointObserver: BreakpointObserver,
     protected sessionService: SessionService,
      @Inject(DOCUMENT) private document: Document,
       private _snackBar: MatSnackBar) {

  }

  ngOnInit(): void {
    this.sessionService.getSessionInformation()
    .subscribe(session => {
      if (!session.isAuthenticated){
        this.document.location.href = session.loginUrl + '?returnurl=' + this.document.location.href;
      }

      this.sessionInformation = session;
    }, 
    err => {
      this._snackBar.open('Error:' + err.message, 'Close');
    });
  }

  logout()
  {
    if (this.sessionInformation) {
      this.document.location.href = this.sessionInformation?.logoutUrl;
    }
  }

}
