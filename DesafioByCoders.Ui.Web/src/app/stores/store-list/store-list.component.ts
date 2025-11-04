import {Component, OnInit} from '@angular/core';
import {StoreService, StoreListItem} from '../store.service';

@Component({
    standalone: false,
    selector: 'app-store-list',
    templateUrl: './store-list.component.html',
    styleUrls: ['./store-list.component.css']
})
export class StoreListComponent implements OnInit
{
    stores: StoreListItem[] = [];
    isLoading = false;
    error: string | null = null;

    constructor(private storeService: StoreService)
    {
    }

    ngOnInit(): void
    {
        this.loadStores();
    }

    loadStores(): void
    {
        this.isLoading = true;
        this.error = null;
        this.storeService.list().subscribe({
            next: (stores) =>
            {
                this.stores = stores;
                this.isLoading = false;
            },
            error: (err) =>
            {
                this.error = 'Failed to load stores. Please try again.';
                this.isLoading = false;
                console.error('Error loading stores:', err);
            }
        });
    }
}
