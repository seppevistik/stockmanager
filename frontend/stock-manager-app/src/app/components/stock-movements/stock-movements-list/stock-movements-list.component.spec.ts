import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StockMovementsListComponent } from './stock-movements-list.component';

describe('StockMovementsListComponent', () => {
  let component: StockMovementsListComponent;
  let fixture: ComponentFixture<StockMovementsListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StockMovementsListComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StockMovementsListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
