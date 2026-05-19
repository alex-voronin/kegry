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
import { LocateFixed } from 'lucide-react';
import { getLowAccuracyLocation } from '@/lib/geo-utils';
import 'leaflet/dist/leaflet.css';
import styles from '../map.module.css';

// Custom dark marker icon as inline SVG
const darkMarkerSvg = encodeURIComponent(
  `<svg xmlns="http://www.w3.org/2000/svg" width="25" height="41" viewBox="0 0 25 41">` +
    `<path d="M12.5 0C5.6 0 0 5.6 0 12.5C0 21.9 12.5 41 12.5 41S25 21.9 25 12.5C25 5.6 19.4 0 12.5 0z" fill="#333333" stroke="rgba(255,255,255,0.3)" stroke-width="1"/>` +
    `<circle cx="12.5" cy="12.5" r="5" fill="#f0f0f0"/>` +
    `</svg>`,
);

const venueMarkerIcon = L.icon({
  iconUrl: `data:image/svg+xml,${darkMarkerSvg}`,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowUrl:
    'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
  shadowSize: [41, 41],
});

export interface VenueMapItem {
  id: string;
  companyName: string;
  companyAvatarImageUrl: string | null;
  address: string;
  latitude: number;
  longitude: number;
}

interface VenueMapInternalProps {
  center: { lat: number; lng: number };
  venues: VenueMapItem[];
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
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [map]);

  return null;
}

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

function createClusterIcon(cluster: any) {
  const count = cluster.getChildCount();
  const size = count < 10 ? 36 : count < 100 ? 42 : 48;

  return L.divIcon({
    html: `<span>${count}</span>`,
    className: 'venue-cluster-icon',
    iconSize: L.point(size, size),
  });
}

const MemoizedMarker = memo(
  function MemoizedMarker({
    venue,
    onVenueDetails,
    detailsLabel,
  }: {
    venue: VenueMapItem;
    onVenueDetails: (id: string) => void;
    detailsLabel: string;
  }) {
    return (
      <Marker
        position={[venue.latitude, venue.longitude]}
        icon={venueMarkerIcon}
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
            <button
              className={styles.markerDetailsBtn}
              onClick={() => onVenueDetails(venue.id)}
            >
              {detailsLabel}
            </button>
          </div>
        </Popup>
      </Marker>
    );
  },
  (prevProps, nextProps) =>
    prevProps.venue.id === nextProps.venue.id &&
    prevProps.detailsLabel === nextProps.detailsLabel,
);

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
  onBoundsChange,
  onVenueDetails,
  detailsLabel,
  initialZoom,
}: VenueMapInternalProps) {
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
            onVenueDetails={onVenueDetails}
            detailsLabel={detailsLabel}
          />
        ))}
      </MarkerClusterGroup>

      <FlyToCenter center={center} />
      <BoundsWatcher onBoundsChange={onBoundsChange} />
      <LocateMe />

      <div className={styles.attribution}>© OpenStreetMap contributors</div>
    </MapContainer>
  );
}
