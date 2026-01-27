// C:\Projects\RotaAppMobile\src\components\DelayReasonModal.tsx

import React, { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  TextInput,
  Alert,
} from 'react-native';
import { Portal, Modal, Button } from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import {
  DelayReasonCategory,
  getDelayReasonCategories,
} from '../types/journey.types';

interface DelayReasonModalProps {
  visible: boolean;
  onDismiss: () => void;
  onSubmit: (category: DelayReasonCategory, reason: string) => Promise<void>;
  newDelay: number;
  cumulativeDelay: number;
  stopName: string;
}

export const DelayReasonModal: React.FC<DelayReasonModalProps> = ({
  visible,
  onDismiss,
  onSubmit,
  newDelay,
  cumulativeDelay,
  stopName,
}) => {
  const [selectedCategory, setSelectedCategory] = useState<DelayReasonCategory | null>(null);
  const [customReason, setCustomReason] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const categories = getDelayReasonCategories();

  const handleSubmit = async () => {
    if (!selectedCategory) {
      Alert.alert('Uyarı', 'Lütfen bir gecikme sebebi seçin.');
      return;
    }

    if (selectedCategory === DelayReasonCategory.Other && !customReason.trim()) {
      Alert.alert('Uyarı', 'Lütfen gecikme sebebini açıklayın.');
      return;
    }

    try {
      setSubmitting(true);
      await onSubmit(selectedCategory, customReason.trim());

      // Reset form
      setSelectedCategory(null);
      setCustomReason('');
      onDismiss();
    } catch (error) {
      Alert.alert('Hata', 'Gecikme sebebi kaydedilemedi. Lütfen tekrar deneyin.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleCancel = () => {
    setSelectedCategory(null);
    setCustomReason('');
    onDismiss();
  };

  return (
    <Portal>
      <Modal
        visible={visible}
        onDismiss={onDismiss}
        contentContainerStyle={styles.modalContainer}
        dismissable={!submitting}
      >
        <ScrollView style={styles.scrollView}>
          {/* Header */}
          <View style={styles.header}>
            <Icon name="clock-alert-outline" size={32} color="#F59E0B" />
            <Text style={styles.title}>Gecikme Sebebi</Text>
            <Text style={styles.subtitle}>{stopName}</Text>
          </View>

          {/* Delay Info */}
          <View style={styles.delayInfo}>
            <View style={styles.delayInfoRow}>
              <Icon name="clock-plus-outline" size={20} color="#EF4444" />
              <Text style={styles.delayInfoLabel}>Yeni Gecikme:</Text>
              <Text style={styles.delayInfoValue}>{newDelay} dk</Text>
            </View>
            <View style={styles.delayInfoRow}>
              <Icon name="clock-outline" size={20} color="#F59E0B" />
              <Text style={styles.delayInfoLabel}>Toplam Gecikme:</Text>
              <Text style={styles.delayInfoValue}>{cumulativeDelay} dk</Text>
            </View>
          </View>

          {/* Info Message */}
          <View style={styles.infoBox}>
            <Icon name="information-outline" size={20} color="#3B82F6" />
            <Text style={styles.infoText}>
              Bu durakta {newDelay} dakika yeni gecikme tespit edildi. Lütfen gecikme sebebini seçin.
            </Text>
          </View>

          {/* Category Selection */}
          <Text style={styles.sectionTitle}>Gecikme Sebebi Seçin:</Text>
          <View style={styles.categoriesContainer}>
            {categories.map((category) => {
              const isSelected = selectedCategory === category.value;
              return (
                <TouchableOpacity
                  key={category.value}
                  style={[
                    styles.categoryButton,
                    isSelected && styles.categoryButtonSelected,
                  ]}
                  onPress={() => setSelectedCategory(category.value)}
                  disabled={submitting}
                >
                  <Icon
                    name={category.icon}
                    size={24}
                    color={isSelected ? '#FFFFFF' : '#3B82F6'}
                    style={styles.categoryIcon}
                  />
                  <Text
                    style={[
                      styles.categoryLabel,
                      isSelected && styles.categoryLabelSelected,
                    ]}
                  >
                    {category.label}
                  </Text>
                  {isSelected && (
                    <Icon name="check-circle" size={20} color="#FFFFFF" />
                  )}
                </TouchableOpacity>
              );
            })}
          </View>

          {/* Custom Reason Input (shown when "Other" is selected) */}
          {selectedCategory === DelayReasonCategory.Other && (
            <View style={styles.customReasonContainer}>
              <Text style={styles.customReasonLabel}>Açıklama:</Text>
              <TextInput
                style={styles.customReasonInput}
                placeholder="Gecikme sebebini açıklayın..."
                placeholderTextColor="#999"
                value={customReason}
                onChangeText={setCustomReason}
                multiline
                numberOfLines={3}
                maxLength={500}
                editable={!submitting}
              />
              <Text style={styles.characterCount}>
                {customReason.length}/500
              </Text>
            </View>
          )}

          {/* Optional Reason Input for other categories */}
          {selectedCategory && selectedCategory !== DelayReasonCategory.Other && (
            <View style={styles.customReasonContainer}>
              <Text style={styles.customReasonLabel}>Ek Açıklama (İsteğe Bağlı):</Text>
              <TextInput
                style={styles.customReasonInput}
                placeholder="Varsa ek detaylar ekleyin..."
                placeholderTextColor="#999"
                value={customReason}
                onChangeText={setCustomReason}
                multiline
                numberOfLines={2}
                maxLength={500}
                editable={!submitting}
              />
            </View>
          )}

          {/* Action Buttons */}
          <View style={styles.actions}>
            <Button
              mode="outlined"
              onPress={handleCancel}
              style={styles.cancelButton}
              disabled={submitting}
            >
              İptal
            </Button>
            <Button
              mode="contained"
              onPress={handleSubmit}
              style={styles.submitButton}
              buttonColor="#10B981"
              loading={submitting}
              disabled={!selectedCategory || submitting}
            >
              Kaydet
            </Button>
          </View>
        </ScrollView>
      </Modal>
    </Portal>
  );
};

const styles = StyleSheet.create({
  modalContainer: {
    backgroundColor: 'white',
    margin: 20,
    borderRadius: 12,
    maxHeight: '85%',
  },
  scrollView: {
    maxHeight: '100%',
  },
  header: {
    alignItems: 'center',
    padding: 20,
    borderBottomWidth: 1,
    borderBottomColor: '#e5e5e5',
  },
  title: {
    fontSize: 20,
    fontWeight: '700',
    color: '#333',
    marginTop: 12,
  },
  subtitle: {
    fontSize: 14,
    color: '#666',
    marginTop: 4,
  },
  delayInfo: {
    backgroundColor: '#FEF3C7',
    padding: 16,
    marginHorizontal: 16,
    marginTop: 16,
    borderRadius: 8,
    borderLeftWidth: 4,
    borderLeftColor: '#F59E0B',
  },
  delayInfoRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 8,
  },
  delayInfoLabel: {
    fontSize: 14,
    color: '#92400E',
    marginLeft: 8,
    flex: 1,
  },
  delayInfoValue: {
    fontSize: 16,
    fontWeight: '700',
    color: '#92400E',
  },
  infoBox: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    backgroundColor: '#DBEAFE',
    padding: 12,
    marginHorizontal: 16,
    marginTop: 12,
    borderRadius: 8,
  },
  infoText: {
    fontSize: 13,
    color: '#1E40AF',
    marginLeft: 8,
    flex: 1,
    lineHeight: 18,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
    marginHorizontal: 16,
    marginTop: 20,
    marginBottom: 12,
  },
  categoriesContainer: {
    paddingHorizontal: 16,
  },
  categoryButton: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 14,
    backgroundColor: '#F9FAFB',
    borderRadius: 8,
    borderWidth: 2,
    borderColor: '#E5E7EB',
    marginBottom: 10,
  },
  categoryButtonSelected: {
    backgroundColor: '#3B82F6',
    borderColor: '#3B82F6',
  },
  categoryIcon: {
    marginRight: 12,
  },
  categoryLabel: {
    fontSize: 15,
    color: '#333',
    flex: 1,
    fontWeight: '500',
  },
  categoryLabelSelected: {
    color: '#FFFFFF',
    fontWeight: '600',
  },
  customReasonContainer: {
    paddingHorizontal: 16,
    marginTop: 16,
  },
  customReasonLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: '#333',
    marginBottom: 8,
  },
  customReasonInput: {
    backgroundColor: '#F9FAFB',
    borderWidth: 1,
    borderColor: '#E5E7EB',
    borderRadius: 8,
    padding: 12,
    fontSize: 14,
    color: '#333',
    textAlignVertical: 'top',
    minHeight: 80,
  },
  characterCount: {
    fontSize: 12,
    color: '#999',
    textAlign: 'right',
    marginTop: 4,
  },
  actions: {
    flexDirection: 'row',
    padding: 16,
    paddingTop: 20,
    gap: 12,
  },
  cancelButton: {
    flex: 1,
  },
  submitButton: {
    flex: 1,
  },
});
