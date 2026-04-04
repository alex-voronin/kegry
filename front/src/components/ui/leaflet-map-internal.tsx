'use client';

import { useEffect, useRef } from 'react';
import {
  MapContainer,
  TileLayer,
  Marker,
  useMapEvents,
  useMap,
} from 'react-leaflet';
import L from 'leaflet';
import { GeoSearchControl, OpenStreetMapProvider } from 'leaflet-geosearch';
import 'leaflet/dist/leaflet.css';
import 'leaflet-geosearch/dist/geosearch.css';
import styles from './leaflet-map-internal.module.css';

// Fix for default marker icons in Next.js SSR environment
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl:
    'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png',
  iconUrl:
    'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png',
  shadowUrl:
    'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
});

interface LeafletMapInternalProps {
  center: { lat: number; lng: number };
  onLocationSelect: (lat: number, lng: number) => void;
}

function SearchAndEvents({
  onLocationSelect,
}: {
  onLocationSelect: (lat: number, lng: number) => void;
}) {
  const map = useMapEvents({
    click(e) {
      onLocationSelect(e.latlng.lat, e.latlng.lng);
    },
  });

  // Ensure map redraws when container might resize
  useEffect(() => {
    const timer = setTimeout(() => {
      map.invalidateSize();
    }, 250);
    return () => clearTimeout(timer);
  }, [map]);

  // Setup leaflet-geosearch
  useEffect(() => {
    const provider = new OpenStreetMapProvider();
    const searchControl = new (GeoSearchControl as any)({
      provider: provider,
      style: 'bar',
      showMarker: false, // We control our own marker
      showPopup: false,
      autoClose: true,
      retainZoomLevel: false,
      animateZoom: true,
      keepResult: true,
      searchLabel: 'Enter address...',
    });

    map.addControl(searchControl);

    // Listen for search complete
    const handleSearch = (result: any) => {
      if (result && result.location) {
        onLocationSelect(result.location.y, result.location.x);
      }
    };

    map.on('geosearch/showlocation', handleSearch);

    return () => {
      map.off('geosearch/showlocation', handleSearch);
      map.removeControl(searchControl);
    };
  }, [map, onLocationSelect]);

  return null;
}

function CenterMap({ center }: { center: { lat: number; lng: number } }) {
  const map = useMap();
  useEffect(() => {
    map.flyTo([center.lat, center.lng], map.getZoom(), {
      animate: true,
      duration: 0.5,
    });
  }, [center, map]);
  return null;
}

export default function LeafletMapInternal({
  center,
  onLocationSelect,
}: LeafletMapInternalProps) {
  return (
    <MapContainer
      center={[center.lat, center.lng]}
      zoom={15}
      style={{ height: '100%', width: '100%', zIndex: 0 }}
      className={styles.mapContainer}
      zoomControl={true}
      attributionControl={false}
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      <Marker position={[center.lat, center.lng]} />
      <CenterMap center={center} />
      <SearchAndEvents onLocationSelect={onLocationSelect} />
    </MapContainer>
  );
}
