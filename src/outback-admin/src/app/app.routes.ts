import { Routes } from '@angular/router';
import { Clients } from './clients/clients';
import { Navigation } from './navigation/navigation';
import { Sessions } from './sessions/sessions';
import { Home } from './home/home';

export const routes: Routes = [
	{
		path: '',
		component: Navigation,
		children: [
			{
				path: '',
				redirectTo: 'home',
				pathMatch: 'full',
			},
			{
				path: 'home',
				component: Home,
			},
			{
				path: 'clients',
				component: Clients,
			},
			{
				path: 'sessions',
				component: Sessions,
			},
		],
	},
	{
		path: '**',
		redirectTo: '',
	},
];
