import { beforeEach, describe, expect, it, vi } from 'vitest';

const apiClientMock = vi.hoisted(() => ({
  get: vi.fn(),
}));

vi.mock('@/lib/api-client', () => ({
  default: apiClientMock,
}));

import { useDashboardStore } from '@/app/b2b/[locale]/dashboard/use-dashboard-store';
import { useVenuesStore } from '@/app/b2c/[locale]/venues/use-venues-store';

describe('API response consumption in stores', () => {
  beforeEach(() => {
    apiClientMock.get.mockReset();
    useDashboardStore.getState().reset();
    useVenuesStore.getState().reset();
  });

  it('reads a nested object response without the result envelope', async () => {
    const venues = [
      {
        venueId: 'venue-1',
        address: 'Street 1',
        dataPoints: [{ label: 'Mon', count: 3 }],
      },
    ];

    apiClientMock.get.mockResolvedValue({ data: { venues } });

    await useDashboardStore.getState().fetchVisits('company-1', 'Daily');

    expect(apiClientMock.get).toHaveBeenCalledWith(
      '/api/v1/b2b/dashboard/visits',
      { params: { companyId: 'company-1', mode: 'Daily' } },
    );
    expect(useDashboardStore.getState().venues).toEqual(venues);
  });

  it('reads a paginated response directly from response.data', async () => {
    const venue = {
      id: 'venue-1',
      companyName: 'Venue',
      companyAvatarImageUrl: null,
      address: 'Street 1',
      distanceMeters: 10,
      googleMapsUrl: 'https://maps.example.com/venue-1',
      tagIds: [],
    };

    apiClientMock.get.mockResolvedValue({
      data: { data: [venue], totalCount: 1 },
    });

    await useVenuesStore.getState().fetchVenues(44.8, 20.4, 0, 20);

    expect(useVenuesStore.getState().venues).toEqual([venue]);
    expect(useVenuesStore.getState().totalCount).toBe(1);
    expect(useVenuesStore.getState().hasMore).toBe(false);
  });
});
