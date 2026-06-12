import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

type MenuItem = {
  label: string;
  path: string;
};

@Component({
  selector: 'app-navigation',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './navigation.html',
  styleUrl: './navigation.css',
})
export class Navigation {
  protected readonly menuItems: MenuItem[] = [
    { label: 'Home', path: '/home' },
    { label: 'Clients', path: '/clients' },
    { label: 'Sessions', path: '/sessions' },
  ];
}
