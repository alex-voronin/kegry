'use client';

import { useEffect, useRef, useCallback, useState, memo } from 'react';
import {
  MapContainer,
  TileLayer,
  Marker,
  Popup,
  useMap,
  useMapEvents,
} from 'react-leaflet';
import L from 'leaflet';
import MarkerClusterGroup from 'react-leaflet-cluster';
import { VenueDisplayTags } from '@/components/b2c/venue-display-tags';
import { LocateFixed } from 'lucide-react';
import { getLowAccuracyLocation } from '@/lib/geo-utils';
import 'leaflet/dist/leaflet.css';
import styles from '../map.module.css';

// Custom dark marker icon as inline SVG
const darkMarkerSvg = encodeURIComponent(
  `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 50 50" width="50" height="50">` +
    `<path d="M4 1l5.6 45.18c.175 1.409 1.399 2.82 2.801 2.82h25.198c1.401 0 2.627-1.411 2.799-2.82l5.602-45.18h-42zm6.3 5.646h29.399l-1.312 11.295h-26.774l-1.313-11.295z" fill="#333333" stroke="rgba(255,255,255,0.3)" stroke-width="1"/>` +
    `</svg>`,
);

const venueMarkerIcon = L.icon({
  iconUrl: `data:image/svg+xml,${darkMarkerSvg}`,
  iconSize: [30, 30],
  iconAnchor: [15, 30],
  popupAnchor: [1, -30],
});

export interface VenueMapItem {
  id: string;
  companyName: string;
  companyAvatarImageUrl: string | null;
  address: string;
  latitude: number;
  longitude: number;
  tagIds: string[];
}

interface VenueMapInternalProps {
  center: { lat: number; lng: number };
  venues: VenueMapItem[];
  eventCountByVenueId?: Record<string, number>;
  flyToCoord?: { lat: number; lng: number; key: number } | null;
  focusedVenue?: VenueMapItem | null;
  onClearFocus?: () => void;
  onBoundsChange: (bounds: {
    minLat: number;
    maxLat: number;
    minLng: number;
    maxLng: number;
  }) => void;
  onVenueDetails: (id: string) => void;
  detailsLabel: string;
  initialZoom: number;
}

export const MAP_VIEWPORT_KEY = 'venue_map_viewport';

function BoundsWatcher({
  onBoundsChange,
}: {
  onBoundsChange: VenueMapInternalProps['onBoundsChange'];
}) {
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const onBoundsChangeRef = useRef(onBoundsChange);
  onBoundsChangeRef.current = onBoundsChange;

  const map = useMapEvents({
    moveend() {
      if (debounceRef.current) clearTimeout(debounceRef.current);
      debounceRef.current = setTimeout(() => {
        const bounds = map.getBounds();
        const center = map.getCenter();
        // Persist viewport for back-navigation
        sessionStorage.setItem(
          MAP_VIEWPORT_KEY,
          JSON.stringify({
            lat: center.lat,
            lng: center.lng,
            zoom: map.getZoom(),
          }),
        );
        onBoundsChangeRef.current({
          minLat: bounds.getSouth(),
          maxLat: bounds.getNorth(),
          minLng: bounds.getWest(),
          maxLng: bounds.getEast(),
        });
      }, 500);
    },
  });

  // Fire initial bounds on mount only
  useEffect(() => {
    const timer = setTimeout(() => {
      map.invalidateSize();
      const bounds = map.getBounds();
      onBoundsChangeRef.current({
        minLat: bounds.getSouth(),
        maxLat: bounds.getNorth(),
        minLng: bounds.getWest(),
        maxLng: bounds.getEast(),
      });
    }, 300);
    return () => clearTimeout(timer);
     
  }, [map]);

  return null;
}

function FlyToTarget({ coord }: { coord: { lat: number; lng: number; key: number } }) {
  const map = useMap();
  useEffect(() => {
    const center = map.getCenter();
    const dz = Math.abs(map.getZoom() - (map.getMaxZoom() - 2));
    const dl = Math.abs(center.lat - coord.lat) + Math.abs(center.lng - coord.lng);
    if (dl < 0.0001 && dz < 1) return;

    map.flyTo([coord.lat, coord.lng], map.getMaxZoom() - 2, {
      animate: true,
      duration: 0.5,
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [coord.key, map]);
  return null;
}

function FocusedVenuePopup({
  venue,
  onClearFocus,
  markerRefs,
}: {
  venue: VenueMapItem;
  onClearFocus: () => void;
  markerRefs: { current: Map<string, L.Marker> };
}) {
  const map = useMap();

  useEffect(() => {
    const timer = setTimeout(() => {
      const marker = markerRefs.current.get(venue.id);
      if (marker) {
        marker.openPopup();
      }
      onClearFocus();
    }, 600);

    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [venue.id, onClearFocus, map]);

  return null;
}

function createClusterIcon(cluster: any) {
  const count = cluster.getChildCount();
  const size = count < 10 ? 36 : count < 100 ? 42 : 48;

  return L.divIcon({
    html: `<span>${count}</span>`,
    className: 'venue-cluster-icon',
    iconSize: L.point(size, size),
  });
}

const createEventCountIcon = (count: number) => {
  if (count <= 0) return venueMarkerIcon;
  const badge = count > 99 ? '99+' : count;
  return L.divIcon({
    html: `<div style="position:relative;display:inline-block"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 50 50" width="30" height="30"><path d="M4 1l5.6 45.18c.175 1.409 1.399 2.82 2.801 2.82h25.198c1.401 0 2.627-1.411 2.799-2.82l5.602-45.18h-42zm6.3 5.646h29.399l-1.312 11.295h-26.774l-1.313-11.295z" fill="#333333" stroke="rgba(255,255,255,0.3)" stroke-width="1"/></svg><div style="position:absolute;top:-6px;right:-6px;background:#ff4444;color:#fff;border-radius:999px;min-width:18px;height:18px;font-size:11px;font-weight:700;display:flex;align-items:center;justify-content:center;padding:0 4px;box-shadow:0 1px 3px rgba(0,0,0,0.5)">${badge}</div></div>`,
    className: '',
    iconSize: [30, 30],
    iconAnchor: [15, 30],
    popupAnchor: [1, -30],
  });
};

const MemoizedMarker = memo(
  function MemoizedMarker({
    venue,
    eventCount,
    onVenueDetails,
    detailsLabel,
    onMarkerReady,
  }: {
    venue: VenueMapItem;
    eventCount: number;
    onVenueDetails: (id: string) => void;
    detailsLabel: string;
    onMarkerReady?: (id: string, marker: L.Marker) => void;
  }) {
    return (
      <Marker
        ref={(ref) => {
          if (ref && onMarkerReady) onMarkerReady(venue.id, ref);
        }}
        position={[venue.latitude, venue.longitude]}
        icon={createEventCountIcon(eventCount)}
      >
        <Popup>
          <div className={styles.markerCard}>
            <div className={styles.markerCardHeader}>
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={
                  venue.companyAvatarImageUrl ||
                  '/assets/common/venues-icon.svg'
                }
                alt={venue.companyName}
                className={styles.markerAvatar}
              />
              <div className={styles.markerInfo}>
                <div className={styles.markerName}>{venue.companyName}</div>
                <div className={styles.markerAddress}>{venue.address}</div>
              </div>
            </div>
            {venue.tagIds?.length > 0 && (
              <VenueDisplayTags tagIds={venue.tagIds} />
            )}
            {eventCount > 0 ? (
              <div className={styles.markerActions}>
                <button
                  className={styles.markerDetailsBtn}
                  onClick={() => onVenueDetails(venue.id)}
                >
                  {detailsLabel}
                </button>
                <button
                  className={styles.markerEventsBtn}
                  onClick={() => onVenueDetails(venue.id)}
                >
                  Events: {eventCount}
                </button>
              </div>
            ) : (
              <button
                className={styles.markerDetailsBtnFull}
                onClick={() => onVenueDetails(venue.id)}
              >
                {detailsLabel}
              </button>
            )}
          </div>
        </Popup>
      </Marker>
    );
  },
  (prevProps, nextProps) =>
    prevProps.venue.id === nextProps.venue.id &&
    prevProps.detailsLabel === nextProps.detailsLabel &&
    prevProps.eventCount === nextProps.eventCount &&
    prevProps.venue.tagIds === nextProps.venue.tagIds,
);

function FlyToCenter({ center }: { center: { lat: number; lng: number } }) {
  const map = useMap();
  const prevCenterRef = useRef({ lat: center.lat, lng: center.lng });

  useEffect(() => {
    if (
      prevCenterRef.current.lat === center.lat &&
      prevCenterRef.current.lng === center.lng
    ) {
      return;
    }
    prevCenterRef.current = { lat: center.lat, lng: center.lng };
    map.flyTo([center.lat, center.lng], map.getZoom(), {
      animate: true,
      duration: 0.5,
    });
  }, [center.lat, center.lng, map]);

  return null;
}

const LOCATE_ZOOM = 15;

function LocateMe() {
  const map = useMap();
  const [isLocating, setIsLocating] = useState(false);

  const handleLocate = useCallback(async () => {
    setIsLocating(true);
    try {
      const { latitude, longitude } = await getLowAccuracyLocation();
      map.flyTo([latitude, longitude], LOCATE_ZOOM, {
        animate: true,
        duration: 0.5,
      });
    } catch {
      // silently fail if location is denied
    } finally {
      setIsLocating(false);
    }
  }, [map]);

  return (
    <button
      type="button"
      onClick={handleLocate}
      disabled={isLocating}
      className={styles.locateBtn}
      aria-label="Center on my location"
    >
      <LocateFixed size={18} />
    </button>
  );
}

export default function VenueMapInternal({
  center,
  venues,
  eventCountByVenueId,
  flyToCoord,
  focusedVenue,
  onClearFocus,
  onBoundsChange,
  onVenueDetails,
  detailsLabel,
  initialZoom,
}: VenueMapInternalProps) {
  const markerRefs = useRef<Map<string, L.Marker>>(new Map());
  const handleMarkerReady = useCallback((id: string, marker: L.Marker) => {
    markerRefs.current.set(id, marker);
  }, []);

  return (
    <MapContainer
      center={[center.lat, center.lng]}
      zoom={initialZoom}
      style={{ height: '100%', width: '100%', zIndex: 0 }}
      zoomControl={true}
      attributionControl={false}
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />

      <MarkerClusterGroup
        chunkedLoading
        iconCreateFunction={createClusterIcon}
        maxClusterRadius={50}
        spiderfyOnMaxZoom
        showCoverageOnHover={false}
      >
        {venues.map((venue) => (
          <MemoizedMarker
            key={venue.id}
            venue={venue}
            eventCount={eventCountByVenueId?.[venue.id] ?? 0}
            onVenueDetails={onVenueDetails}
            detailsLabel={detailsLabel}
            onMarkerReady={handleMarkerReady}
          />
        ))}
      </MarkerClusterGroup>

      <FlyToCenter center={center} />
      {flyToCoord && <FlyToTarget coord={flyToCoord} />}
      {focusedVenue && (
        <FocusedVenuePopup
          venue={focusedVenue}
          onClearFocus={onClearFocus ?? (() => {})}
          markerRefs={markerRefs}
        />
      )}
      <BoundsWatcher onBoundsChange={onBoundsChange} />
      <LocateMe />

      <div className={styles.attribution}>© OpenStreetMap contributors</div>
    </MapContainer>
  );
}
