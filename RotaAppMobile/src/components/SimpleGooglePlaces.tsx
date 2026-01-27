import React, { useState, useRef } from 'react';
import { View, StyleSheet, TouchableOpacity, ScrollView, Dimensions, TextInput as RNTextInput } from 'react-native';
import { Text, Surface, ActivityIndicator } from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { GOOGLE_MAPS_API_KEY } from '../config/google';

const { height: SCREEN_HEIGHT } = Dimensions.get('window');

interface Place {
  place_id: string;
  description: string;
  name?: string;
  formatted_address?: string;
  geometry?: {
    location: {
      lat: number;
      lng: number;
    };
  };
  formatted_phone_number?: string;
  international_phone_number?: string;
  website?: string;
  types?: string[];
  structured_formatting?: {
    main_text: string;
    secondary_text: string;
  };
}

interface GooglePlacesComponentProps {
  placeholder?: string;
  onPlaceSelect: (place: Place, details?: any) => void;
  style?: any;
}

const GooglePlacesComponent: React.FC<GooglePlacesComponentProps> = ({
  placeholder = "İşletme adı yazın (örn: Migros, Starbucks)",
  onPlaceSelect,
  style,
}) => {
  const [query, setQuery] = useState('');
  const [suggestions, setSuggestions] = useState<Place[]>([]);
  const [loading, setLoading] = useState(false);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const searchTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  const searchPlaces = async (text: string) => {
    if (text.length < 2) {
      setSuggestions([]);
      setShowSuggestions(false);
      return;
    }

    setLoading(true);
    try {
      // Türkçe karakterlerin proper encoding'i için
      const encodedText = encodeURIComponent(text);
      console.log('📍 Encoded search text:', encodedText);
      
      const url = `https://maps.googleapis.com/maps/api/place/autocomplete/json?input=${encodedText}&key=${GOOGLE_MAPS_API_KEY}&language=tr&components=country:tr&types=establishment`;
      
      const response = await fetch(url, {
        method: 'GET',
        headers: {
          'Accept': 'application/json; charset=utf-8',
          'Content-Type': 'application/json; charset=utf-8',
          'Accept-Charset': 'utf-8',
        },
      });
      const data = await response.json();
      
      if (data.predictions && data.status === 'OK') {
        setSuggestions(data.predictions.slice(0, 5)); // İlk 5 sonuç
        setShowSuggestions(true);
        console.log('✅ Google Places: Found', data.predictions.length, 'results');
      } else {
        setSuggestions([]);
        setShowSuggestions(false);
        console.log('❌ Google Places: No results or error:', data.status);
      }
    } catch (error) {
      console.error('Google Places error:', error);
      setSuggestions([]);
      setShowSuggestions(false);
    } finally {
      setLoading(false);
    }
  };

  const getPlaceDetails = async (placeId: string): Promise<Place | null> => {
    try {
      const fields = [
        'name', 
        'formatted_address', 
        'geometry', 
        'formatted_phone_number', 
        'international_phone_number',
        'website',
        'types'
      ].join(',');
      
      const response = await fetch(
        `https://maps.googleapis.com/maps/api/place/details/json?place_id=${placeId}&key=${GOOGLE_MAPS_API_KEY}&fields=${fields}&language=tr`
      );
      const data = await response.json();
      
      if (data.result && data.status === 'OK') {
        return data.result;
      }
      return null;
    } catch (error) {
      console.error('Place details error:', error);
      return null;
    }
  };

  const handleTextChange = (text: string) => {
    setQuery(text);
    
    // Clear previous timeout
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }
    
    // Debounce search
    searchTimeoutRef.current = setTimeout(() => {
      const trimmedText = text.trim();
      if (trimmedText.length >= 2) {
        console.log('🔍 Searching for:', trimmedText);
        searchPlaces(trimmedText);
      } else {
        setSuggestions([]);
        setShowSuggestions(false);
      }
    }, 300);
  };

  const handlePlaceSelect = async (place: Place) => {
    // Display selected place name
    setQuery(place.structured_formatting?.main_text || place.name || place.description);
    setShowSuggestions(false);
    setSuggestions([]);
    
    // Get detailed information
    const details = await getPlaceDetails(place.place_id);
    console.log('Place details:', details);
    
    // Web implementasyonuna uygun şekilde veri hazırlayalım
    const enrichedPlace = {
      ...place,
      ...details, // Details ile birleştir
    };
    
    onPlaceSelect(enrichedPlace, enrichedPlace);
  };

  const clearSearch = () => {
    setQuery('');
    setSuggestions([]);
    setShowSuggestions(false);
  };

  // Map işletme tiplerini Türkçe tag'lere
  const getBusinessTags = (types?: string[]): string[] => {
    if (!types) return [];
    
    const typeMapping: Record<string, string> = {
      'restaurant': 'Restoran',
      'food': 'Yemek',
      'store': 'Mağaza', 
      'supermarket': 'Süpermarket',
      'gas_station': 'Benzin İstasyonu',
      'hospital': 'Hastane',
      'pharmacy': 'Eczane',
      'bank': 'Banka',
      'atm': 'ATM',
      'shopping_mall': 'AVM',
      'cafe': 'Kafe',
      'bakery': 'Fırın',
      'clothing_store': 'Giyim',
      'electronics_store': 'Elektronik',
    };

    return types
      .map(type => typeMapping[type])
      .filter(Boolean)
      .slice(0, 3); // İlk 3 tag
  };

  return (
    <View style={[styles.container, style]}>
      <View style={styles.inputContainer}>
        <RNTextInput
          placeholder={placeholder}
          value={query}
          onChangeText={handleTextChange}
          style={styles.simpleInput}
        />
        {loading && (
          <View style={styles.loadingIndicator}>
            <ActivityIndicator size={16} color="#666" />
          </View>
        )}
        {query.length > 0 && !loading && (
          <TouchableOpacity onPress={clearSearch} style={styles.clearButton}>
            <Icon name="close" size={20} color="#666" />
          </TouchableOpacity>
        )}
        
        {/* Absolute positioned suggestions - Input'un hemen altında */}
        {showSuggestions && suggestions.length > 0 && (
          <Surface style={styles.suggestionsList}>
            <ScrollView 
              style={styles.suggestionsScroll}
              keyboardShouldPersistTaps="handled"
              nestedScrollEnabled={true}
              showsVerticalScrollIndicator={true}
            >
              {suggestions.map((item, index) => (
                <TouchableOpacity
                  key={item.place_id}
                  style={[
                    styles.suggestionItem,
                    index === suggestions.length - 1 && styles.lastSuggestionItem
                  ]}
                  onPress={() => handlePlaceSelect(item)}
                  activeOpacity={0.7}
                >
                  <View style={styles.suggestionContent}>
                    <Icon name="map-marker" size={16} color="#3B82F6" style={styles.suggestionIcon} />
                    <View style={styles.suggestionTexts}>
                      <Text style={styles.mainText} numberOfLines={1}>
                        {item.structured_formatting?.main_text || item.description}
                      </Text>
                      {item.structured_formatting?.secondary_text && (
                        <Text style={styles.secondaryText} numberOfLines={1}>
                          {item.structured_formatting.secondary_text}
                        </Text>
                      )}
                    </View>
                  </View>
                </TouchableOpacity>
              ))}
            </ScrollView>
          </Surface>
        )}
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    // Çok önemli: Bu container'ın zIndex'i yüksek olmalı
    zIndex: 1000,
    elevation: 1000,
  },
  inputContainer: {
    position: 'relative',
    zIndex: 1000, // InputContainer'ın da zIndex'i yüksek
  },
  simpleInput: {
    height: 50,
    borderWidth: 1,
    borderColor: '#DADCE0',
    borderRadius: 4,
    paddingHorizontal: 16,
    fontSize: 16,
    backgroundColor: 'white',
    color: '#000',
  },
  loadingIndicator: {
    position: 'absolute',
    right: 16,
    top: 15,
  },
  clearButton: {
    position: 'absolute',
    right: 16,
    top: 15,
    padding: 4,
  },
  suggestionsList: {
    position: 'absolute',
    top: '100%', // Input'un hemen altında
    left: 0,
    right: 0,
    backgroundColor: 'white',
    borderRadius: 8,
    marginTop: 4,
    // Çok yüksek z-index ve elevation
    zIndex: 2000,
    elevation: 1500,
    // Güçlü shadow efektleri
    shadowColor: '#000',
    shadowOffset: {
      width: 0,
      height: 8,
    },
    shadowOpacity: 0.3,
    shadowRadius: 12,
    // Border ekle
    borderWidth: 1,
    borderColor: '#E5E7EB',
  },
  suggestionsScroll: {
    maxHeight: 180, // Sabit yükseklik - çok büyük olmasın
  },
  suggestionItem: {
    borderBottomWidth: 1,
    borderBottomColor: '#f0f0f0',
  },
  lastSuggestionItem: {
    borderBottomWidth: 0,
  },
  suggestionContent: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 10, // Biraz daha compact
  },
  suggestionIcon: {
    marginRight: 8,
  },
  suggestionTexts: {
    flex: 1,
  },
  mainText: {
    fontSize: 14,
    fontWeight: '500',
    color: '#333',
  },
  secondaryText: {
    fontSize: 12,
    color: '#666',
    marginTop: 2,
  },
});

export default GooglePlacesComponent;
