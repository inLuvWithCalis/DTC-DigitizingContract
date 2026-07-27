import * as React from 'react';
import { cn } from '@/lib/utils';
import { Platform, TextInput, View, Pressable, Text } from 'react-native';
import { X } from 'lucide-react-native';
import { useColorScheme } from 'nativewind';

interface InputProps extends React.ComponentProps<typeof TextInput> {
  onClear?: () => void;
}

const Input = React.forwardRef<TextInput, InputProps>(
  ({ className, onClear, onChangeText, value, defaultValue, maxLength, ...props }, ref) => {
    const { colorScheme } = useColorScheme();
    const isDark = colorScheme === 'dark';

    // Internal state to track length when used uncontrolled
    const [internalValue, setInternalValue] = React.useState(() => {
      if (value !== undefined) return String(value);
      if (defaultValue !== undefined) return String(defaultValue);
      return '';
    });

    React.useEffect(() => {
      if (value !== undefined) {
        setInternalValue(String(value));
      }
    }, [value]);

    const handleChangeText = (text: string) => {
      if (value === undefined) {
        setInternalValue(text);
      }
      if (onChangeText) {
        onChangeText(text);
      }
    };

    const handleClear = () => {
      if (value === undefined) {
        setInternalValue('');
      }
      if (onChangeText) {
        onChangeText('');
      }
      if (onClear) {
        onClear();
      }
    };

    const charCount = internalValue.length;
    const showClearButton = charCount > 0 && props.editable !== false;

    return (
      <View className="relative w-full justify-center">
        <TextInput
          ref={ref}
          value={value}
          defaultValue={defaultValue}
          onChangeText={handleChangeText}
          maxLength={maxLength}
          className={cn(
            'dark:bg-input/30 border-input bg-background text-foreground flex h-10 w-full min-w-0 flex-row items-center rounded-md border px-3 py-1 text-base leading-5 shadow-sm shadow-black/5 sm:h-9',
            props.editable === false &&
              cn(
                'opacity-50',
                Platform.select({ web: 'disabled:pointer-events-none disabled:cursor-not-allowed' })
              ),
            Platform.select({
              web: cn(
                'placeholder:text-muted-foreground selection:bg-primary selection:text-primary-foreground outline-none transition-[color,box-shadow] md:text-sm',
                'focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]',
                'aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive'
              ),
              native: 'placeholder:text-muted-foreground/50',
            }),
            showClearButton && maxLength
              ? 'pr-[72px]'
              : maxLength
                ? 'pr-[60px]'
                : showClearButton
                  ? 'pr-[36px]'
                  : 'pr-3',
            className
          )}
          {...props}
        />

        <View className="absolute right-2 top-0 bottom-0 flex-row items-center justify-center gap-1.5 z-10">
          {showClearButton && (
            <Pressable
              onPress={handleClear}
              className="p-1 rounded-full active:bg-muted justify-center items-center"
              hitSlop={8}
            >
              <X size={16} color={isDark ? '#9ca3af' : '#6b7280'} />
            </Pressable>
          )}

          {maxLength && (
            <View className="min-w-[28px] shrink-0 items-end">
              <Text className="text-[11px] font-medium text-muted-foreground/70">
                {charCount}/{maxLength}
              </Text>
            </View>
          )}
        </View>
      </View>
    );
  }
);

Input.displayName = 'Input';

export { Input };
