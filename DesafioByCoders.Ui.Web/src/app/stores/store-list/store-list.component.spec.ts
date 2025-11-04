import {ComponentFixture, TestBed, fakeAsync, tick} from '@angular/core/testing';
import {StoreListComponent} from './store-list.component';
import {HttpClientTestingModule} from '@angular/common/http/testing';
import {CommonModule} from '@angular/common';
import {StoreService, StoreListItem} from '../store.service';
import {of, throwError} from 'rxjs';

describe('StoreListComponent', () => {
    let component: StoreListComponent;
    let fixture: ComponentFixture<StoreListComponent>;
    let storeService: jasmine.SpyObj<StoreService>;

    beforeEach(async () => {
        const storeServiceSpy = jasmine.createSpyObj('StoreService', ['list']);
        storeServiceSpy.list.and.returnValue(of([]));

        await TestBed.configureTestingModule({
            declarations: [StoreListComponent],
            imports: [CommonModule, HttpClientTestingModule],
            providers: [
                {provide: StoreService, useValue: storeServiceSpy}
            ]
        }).compileComponents();

        storeService = TestBed.inject(StoreService) as jasmine.SpyObj<StoreService>;
        fixture = TestBed.createComponent(StoreListComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    describe('ngOnInit', () => {
        it('should load stores on initialization', () => {
            const mockStores: StoreListItem[] = [
                {id: 1, name: 'Store A', owner: 'Owner A', balance: 100},
                {id: 2, name: 'Store B', owner: 'Owner B', balance: -50}
            ];
            storeService.list.and.returnValue(of(mockStores));
            fixture.detectChanges();
            expect(storeService.list).toHaveBeenCalled();
            expect(component.stores).toEqual(mockStores);
            expect(component.isLoading).toBe(false);
            expect(component.error).toBeNull();
        });

        it('should set loading to false after fetching stores', () => {
            storeService.list.and.returnValue(of([]));
            fixture.detectChanges();
            expect(component.isLoading).toBe(false);
            expect(component.error).toBeNull();
        });
    });

    describe('loadStores', () => {
        it('should load stores successfully', () => {
            const mockStores: StoreListItem[] = [
                {id: 1, name: 'Store A', owner: 'Owner A', balance: 100},
                {id: 2, name: 'Store B', owner: 'Owner B', balance: -50}
            ];
            storeService.list.and.returnValue(of(mockStores));
            component.loadStores();
            expect(component.stores).toEqual(mockStores);
            expect(component.isLoading).toBe(false);
            expect(component.error).toBeNull();
        });

        it('should handle empty store list', () => {
            storeService.list.and.returnValue(of([]));
            component.loadStores();
            expect(component.stores).toEqual([]);
            expect(component.isLoading).toBe(false);
            expect(component.error).toBeNull();
        });

        it('should handle error when loading stores fails', () => {
            const errorResponse = {status: 500, statusText: 'Server Error'};
            storeService.list.and.returnValue(throwError(() => errorResponse));
            spyOn(console, 'error');
            component.loadStores();
            expect(component.error).toBe('Failed to load stores. Please try again.');
            expect(component.isLoading).toBe(false);
            expect(console.error).toHaveBeenCalledWith('Error loading stores:', errorResponse);
        });

        it('should reset error state when reloading', () => {
            component.error = 'Previous error';
            storeService.list.and.returnValue(of([]));
            component.loadStores();
            expect(component.error).toBeNull();
        });
    });

    describe('template rendering', () => {
        beforeEach(() => {
            // Let ngOnInit run with default empty result
            fixture.detectChanges();
        });

        it('should display loading message when isLoading is true', () => {
            component.isLoading = true;
            fixture.detectChanges();
            const loadingEl = fixture.nativeElement.querySelector('.loading');
            expect(loadingEl).toBeTruthy();
            expect(loadingEl.textContent).toContain('Loading stores...');
        });

        it('should display error message when error exists', () => {
            component.error = 'Failed to load stores. Please try again.';
            component.isLoading = false;
            fixture.detectChanges();
            const errorEl = fixture.nativeElement.querySelector('.error');
            expect(errorEl).toBeTruthy();
            expect(errorEl.textContent).toContain('Failed to load stores. Please try again.');
            const retryButton = errorEl.querySelector('button');
            expect(retryButton).toBeTruthy();
        });

        it('should display stores table when stores are loaded', () => {
            component.stores = [
                {id: 1, name: 'Store A', owner: 'Owner A', balance: 100},
                {id: 2, name: 'Store B', owner: 'Owner B', balance: -50}
            ];
            component.isLoading = false;
            component.error = null;
            fixture.detectChanges();
            const rows = fixture.nativeElement.querySelectorAll('tbody tr');
            expect(rows.length).toBe(2);
            expect(rows[0].textContent).toContain('Store A');
            expect(rows[0].textContent).toContain('Owner A');
            expect(rows[1].textContent).toContain('Store B');
        });

        it('should display no data message when stores array is empty', () => {
            component.stores = [];
            component.isLoading = false;
            component.error = null;
            fixture.detectChanges();
            const noDataEl = fixture.nativeElement.querySelector('.file-name');
            expect(noDataEl).toBeTruthy();
            expect(noDataEl.textContent).toContain('No stores to display yet.');
        });

        it('should call loadStores when retry button is clicked', () => {
            component.error = 'Test error';
            component.isLoading = false;
            fixture.detectChanges();
            spyOn(component, 'loadStores');
            const retryButton = fixture.nativeElement.querySelector('.error button');
            expect(retryButton).toBeTruthy();
            retryButton!.click();
            expect(component.loadStores).toHaveBeenCalled();
        });

        it('should display positive balance with correct styling', fakeAsync(() => {
            component.stores = [
                {id: 1, name: 'Store A', owner: 'Owner A', balance: 150.50}
            ];
            component.isLoading = false;
            component.error = null;
            fixture.detectChanges();
            tick();
            fixture.detectChanges();
            const table = fixture.nativeElement.querySelector('table');
            expect(table).toBeTruthy();
            const span = fixture.nativeElement.querySelector('tbody tr td span.balance-amount');
            if (!span) {
                // diagnostic output
                // eslint-disable-next-line no-console
                console.log('HTML snapshot positive:', fixture.nativeElement.innerHTML);
            }
            expect(span).toBeTruthy();
            expect(span!.classList.contains('negative')).toBe(false);
        }));

        it('should display negative balance with correct styling', () => {
            component.stores = [
                {id: 1, name: 'Store A', owner: 'Owner A', balance: -75.25}
            ];
            component.isLoading = false;
            component.error = null;
            fixture.detectChanges();
            const span = fixture.nativeElement.querySelector('span.balance-amount.negative');
            expect(span).toBeTruthy();
        });

        it('should display zero balance correctly', fakeAsync(() => {
            component.stores = [
                {id: 1, name: 'Store A', owner: 'Owner A', balance: 0}
            ];
            component.isLoading = false;
            component.error = null;
            fixture.detectChanges();
            tick();
            fixture.detectChanges();
            const span = fixture.nativeElement.querySelector('tbody tr td span.balance-amount');
            if (!span) {
                // eslint-disable-next-line no-console
                console.log('HTML snapshot zero:', fixture.nativeElement.innerHTML);
            }
            expect(span).toBeTruthy();
            expect(span!.textContent).toContain('$0.00');
        }));

        it('should display multiple stores in correct order', () => {
            component.stores = [
                {id: 1, name: 'Store A', owner: 'Owner A', balance: 100},
                {id: 2, name: 'Store B', owner: 'Owner B', balance: 200},
                {id: 3, name: 'Store C', owner: 'Owner C', balance: 300}
            ];
            component.isLoading = false;
            component.error = null;
            fixture.detectChanges();
            const rows = fixture.nativeElement.querySelectorAll('tbody tr');
            expect(rows.length).toBe(3);
            expect(rows[0].textContent).toContain('Store A');
            expect(rows[1].textContent).toContain('Store B');
            expect(rows[2].textContent).toContain('Store C');
        });

        it('should not display table when loading is true', () => {
            component.isLoading = true;
            component.error = null;
            component.stores = [
                {id: 1, name: 'Store A', owner: 'Owner A', balance: 100}
            ];
            fixture.detectChanges();
            const table = fixture.nativeElement.querySelector('table');
            expect(table).toBeFalsy();
        });

        it('should not display table when error exists', () => {
            component.error = 'Error message';
            component.isLoading = false;
            component.stores = [
                {id: 1, name: 'Store A', owner: 'Owner A', balance: 100}
            ];
            fixture.detectChanges();
            const table = fixture.nativeElement.querySelector('table');
            expect(table).toBeFalsy();
        });
    });

    describe('state transitions', () => {
        it('should transition from loading to success state', () => {
            const mockStores: StoreListItem[] = [
                {id: 1, name: 'Store A', owner: 'Owner A', balance: 100}
            ];
            storeService.list.and.returnValue(of(mockStores));
            component.loadStores();
            expect(component.stores).toEqual(mockStores);
            expect(component.isLoading).toBe(false);
            expect(component.error).toBeNull();
        });

        it('should transition from loading to error state', () => {
            const errorResponse = {status: 500, statusText: 'Server Error'};
            storeService.list.and.returnValue(throwError(() => errorResponse));
            spyOn(console, 'error');
            component.loadStores();
            expect(component.stores).toEqual([]);
            expect(component.isLoading).toBe(false);
            expect(component.error).toBe('Failed to load stores. Please try again.');
        });

        it('should transition from error state to success state on retry', () => {
            const mockStores: StoreListItem[] = [
                {id: 1, name: 'Store A', owner: 'Owner A', balance: 100}
            ];
            storeService.list.and.returnValue(throwError(() => ({status: 500})));
            spyOn(console, 'error');
            component.loadStores();
            expect(component.error).toBe('Failed to load stores. Please try again.');
            storeService.list.and.returnValue(of(mockStores));
            component.loadStores();
            expect(component.error).toBeNull();
            expect(component.stores).toEqual(mockStores);
            expect(component.isLoading).toBe(false);
        });

        it('should clear stores array when reloading', () => {
            const initialStores: StoreListItem[] = [
                {id: 1, name: 'Store A', owner: 'Owner A', balance: 100}
            ];
            component.stores = initialStores;
            storeService.list.and.returnValue(of([]));
            component.loadStores();
            expect(component.stores).toEqual([]);
        });
    });

    describe('error handling', () => {
        it('should handle network timeout error', () => {
            const timeoutError = {name: 'TimeoutError', message: 'Timeout has occurred'};
            storeService.list.and.returnValue(throwError(() => timeoutError));
            spyOn(console, 'error');
            component.loadStores();
            expect(component.error).toBe('Failed to load stores. Please try again.');
            expect(component.isLoading).toBe(false);
            expect(console.error).toHaveBeenCalledWith('Error loading stores:', timeoutError);
        });

        it('should handle 404 not found error', () => {
            const notFoundError = {status: 404, statusText: 'Not Found'};
            storeService.list.and.returnValue(throwError(() => notFoundError));
            spyOn(console, 'error');
            component.loadStores();
            expect(component.error).toBe('Failed to load stores. Please try again.');
            expect(component.isLoading).toBe(false);
        });

        it('should handle 403 forbidden error', () => {
            const forbiddenError = {status: 403, statusText: 'Forbidden'};
            storeService.list.and.returnValue(throwError(() => forbiddenError));
            spyOn(console, 'error');
            component.loadStores();
            expect(component.error).toBe('Failed to load stores. Please try again.');
            expect(component.isLoading).toBe(false);
        });
    });

    describe('data integrity', () => {
        beforeEach(() => {
            fixture.detectChanges();
        });

        it('should handle stores with very large balances', () => {
            component.stores = [
                {id: 1, name: 'Store A', owner: 'Owner A', balance: 999999.99}
            ];
            component.isLoading = false;
            component.error = null;
            fixture.detectChanges();
            const compiled = fixture.nativeElement as HTMLElement;
            expect(compiled.textContent).toContain('999,999.99');
        });

        it('should handle stores with very small negative balances', () => {
            component.stores = [
                {id: 1, name: 'Store A', owner: 'Owner A', balance: -0.01}
            ];
            component.isLoading = false;
            component.error = null;
            fixture.detectChanges();
            const span = fixture.nativeElement.querySelector('span.balance-amount.negative');
            expect(span).toBeTruthy();
        });

        it('should handle stores with special characters in names', () => {
            component.stores = [
                {id: 1, name: "Store A & B's Shop", owner: "O'Connor", balance: 100}
            ];
            component.isLoading = false;
            component.error = null;
            fixture.detectChanges();
            const compiled = fixture.nativeElement as HTMLElement;
            expect(compiled.textContent).toContain("Store A & B's Shop");
            expect(compiled.textContent).toContain("O'Connor");
        });

        it('should handle stores with long names', () => {
            component.stores = [
                {id: 1, name: 'Store with a Very Long Name That Might Cause Layout Issues', owner: 'Owner', balance: 100}
            ];
            component.isLoading = false;
            component.error = null;
            fixture.detectChanges();
            const compiled = fixture.nativeElement as HTMLElement;
            expect(compiled.textContent).toContain('Store with a Very Long Name');
        });
    });
});
