import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { StoreService, StoreListItem } from './store.service';

describe('StoreService', () => {
  let service: StoreService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [StoreService]
    });
    service = TestBed.inject(StoreService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('list', () => {
    it('should return an Observable of StoreListItem array', () => {
      const mockStores: StoreListItem[] = [
        { id: 1, name: 'Store A', owner: 'Owner A', balance: 100.50 },
        { id: 2, name: 'Store B', owner: 'Owner B', balance: -50.25 },
        { id: 3, name: 'Store C', owner: 'Owner C', balance: 0 }
      ];

      service.list().subscribe(stores => {
        expect(stores).toEqual(mockStores);
        expect(stores.length).toBe(3);
        expect(stores[0].name).toBe('Store A');
        expect(stores[1].balance).toBe(-50.25);
      });

      const req = httpMock.expectOne('/api/v1/stores');
      expect(req.request.method).toBe('GET');
      req.flush(mockStores);
    });

    it('should return an empty array when no stores exist', () => {
      const mockStores: StoreListItem[] = [];

      service.list().subscribe(stores => {
        expect(stores).toEqual([]);
        expect(stores.length).toBe(0);
      });

      const req = httpMock.expectOne('/api/v1/stores');
      expect(req.request.method).toBe('GET');
      req.flush(mockStores);
    });

    it('should handle HTTP error gracefully', () => {
      const errorMessage = 'Server error';

      service.list().subscribe({
        next: () => fail('should have failed with server error'),
        error: (error) => {
          expect(error.status).toBe(500);
          expect(error.statusText).toBe('Server Error');
        }
      });

      const req = httpMock.expectOne('/api/v1/stores');
      req.flush(errorMessage, { status: 500, statusText: 'Server Error' });
    });

    it('should handle network error', () => {
      service.list().subscribe({
        next: () => fail('should have failed with network error'),
        error: (error) => {
          expect(error.error.type).toBe('error');
        }
      });

      const req = httpMock.expectOne('/api/v1/stores');
      req.error(new ProgressEvent('error'));
    });
  });
});
